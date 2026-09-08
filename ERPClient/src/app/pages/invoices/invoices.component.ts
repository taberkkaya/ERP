import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ConfirmService } from '../../core/confirm.service';
import { MoneyPipe, QuantityPipe, TrDatePipe, toInputDate } from '../../core/format';
import { HttpService } from '../../core/http.service';
import { ToastService } from '../../core/toast.service';
import {
  CustomerModel,
  DepotModel,
  InvoiceDetailModel,
  InvoiceDraft,
  InvoiceModel,
  InvoiceType,
  ProductModel,
} from '../../models/domain.model';
import { IconComponent } from '../../ui/icon.component';
import { ModalComponent } from '../../ui/modal.component';
import { EmptyComponent, PageComponent, PanelComponent, SearchComponent } from '../../ui/primitives';

@Component({
  selector: 'tz-invoices',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MoneyPipe,
    QuantityPipe,
    TrDatePipe,
    IconComponent,
    ModalComponent,
    PageComponent,
    PanelComponent,
    EmptyComponent,
    SearchComponent,
  ],
  templateUrl: './invoices.component.html',
})
export class InvoicesComponent implements OnInit {
  private readonly http = inject(HttpService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly typeValue = signal<number>(InvoiceType.Purchase);
  readonly invoices = signal<InvoiceModel[]>([]);
  readonly customers = signal<CustomerModel[]>([]);
  readonly products = signal<ProductModel[]>([]);
  readonly depots = signal<DepotModel[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly submitted = signal(false);

  readonly editing = signal<InvoiceDraft | null>(null);
  readonly isNew = signal(true);

  readonly lineProductId = signal('');
  readonly lineDepotId = signal('');
  readonly lineQuantity = signal<number | null>(null);
  readonly linePrice = signal<number | null>(null);

  readonly isPurchase = computed(() => this.typeValue() === InvoiceType.Purchase);

  readonly heading = computed(() => (this.isPurchase() ? 'Alış Faturaları' : 'Satış Faturaları'));

  readonly description = computed(() =>
    this.isPurchase()
      ? 'Tedarikçiden gelen kalemler; kaydedildiğinde seçilen depoya stok girişi yapılır.'
      : 'Müşteriye çıkan kalemler; kaydedildiğinde seçilen depodan stok çıkışı yapılır.'
  );

  readonly icon = computed(() => (this.isPurchase() ? 'invoice-in' : 'invoice-out'));

  readonly filtered = computed(() => {
    const term = this.search().trim().toLocaleLowerCase('tr');
    if (!term) return this.invoices();

    return this.invoices().filter((invoice) =>
      `${invoice.invoiceNumber} ${invoice.customer?.name ?? ''}`
        .toLocaleLowerCase('tr')
        .includes(term)
    );
  });

  readonly grandTotal = computed(() =>
    this.invoices().reduce((sum, invoice) => sum + this.total(invoice), 0)
  );

  ngOnInit(): void {
    // Aynı bileşen iki rotayı da karşılıyor; alış ile satış arasında geçişte
    // yeniden kurulmadığı için parametre akış olarak dinleniyor.
    this.route.paramMap.subscribe((params) => {
      this.typeValue.set(params.get('type') === 'satis' ? InvoiceType.Sales : InvoiceType.Purchase);
      this.search.set('');
      this.editing.set(null);
      this.load();
    });
  }

  load(): void {
    this.loading.set(true);

    forkJoin({
      invoices: this.http.request<InvoiceModel[]>('Invoices/GetAll', { type: this.typeValue() }),
      customers: this.http.request<CustomerModel[]>('Customers/GetAll'),
      products: this.http.request<ProductModel[]>('Products/GetAll'),
      depots: this.http.request<DepotModel[]>('Depots/GetAll'),
    }).subscribe({
      next: ({ invoices, customers, products, depots }) => {
        this.invoices.set(invoices ?? []);
        this.customers.set(customers ?? []);
        this.products.set(products ?? []);
        this.depots.set(depots ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openCreate(): void {
    const model: InvoiceDraft = { ...new InvoiceModel(), details: [] };
    model.date = toInputDate();
    model.typeValue = this.typeValue();

    this.submitted.set(false);
    this.isNew.set(true);
    this.resetLineDraft();
    this.editing.set(model);
  }

  openEdit(invoice: InvoiceModel): void {
    this.submitted.set(false);
    this.isNew.set(false);
    this.resetLineDraft();

    this.editing.set({
      ...invoice,
      date: invoice.date ? invoice.date.slice(0, 10) : toInputDate(),
      typeValue: this.typeValue(),
      details: (invoice.details ?? []).map((detail) => ({ ...detail })),
    });
  }

  close(): void {
    this.editing.set(null);
  }

  addLine(): void {
    const model = this.editing();
    const productId = this.lineProductId();
    const depotId = this.lineDepotId();
    const quantity = Number(this.lineQuantity());
    const price = Number(this.linePrice());

    if (!model) return;

    if (!productId || !depotId || !(quantity > 0)) {
      this.toast.warn('Ürün, depo ve sıfırdan büyük bir miktar seçin.');
      return;
    }

    const line: InvoiceDetailModel = {
      ...new InvoiceDetailModel(),
      productId,
      depotId,
      product: this.products().find((item) => item.id === productId) ?? new ProductModel(),
      depot: this.depots().find((item) => item.id === depotId) ?? new DepotModel(),
      quantity,
      price: Number.isFinite(price) ? price : 0,
    };

    this.editing.set({ ...model, details: [...model.details, line] });
    this.resetLineDraft();
  }

  removeLine(index: number): void {
    const model = this.editing();
    if (!model) return;

    this.editing.set({ ...model, details: model.details.filter((_, i) => i !== index) });
  }

  total(invoice: InvoiceModel): number {
    return (invoice.details ?? []).reduce(
      (sum, detail) => sum + detail.quantity * detail.price,
      0
    );
  }

  save(form: NgForm): void {
    this.submitted.set(true);

    const model = this.editing();
    if (!model || form.invalid) return;

    if (!model.details.length) {
      this.toast.warn('Faturaya en az bir kalem ekleyin.');
      return;
    }

    const details = model.details.map((detail) => ({
      productId: detail.productId,
      depotId: detail.depotId,
      quantity: detail.quantity,
      price: detail.price,
    }));

    const isNew = this.isNew();
    const body = isNew
      ? {
          customerId: model.customerId,
          typeValue: this.typeValue(),
          date: model.date,
          invoiceNumber: model.invoiceNumber,
          details,
          orderId: model.orderId ?? null,
        }
      : {
          id: model.id,
          date: model.date,
          invoiceNumber: model.invoiceNumber,
          details,
        };

    this.http.post<string>(isNew ? 'Invoices/Create' : 'Invoices/Update', body, (message) => {
      this.toast.ok(message);
      this.close();
      this.load();
    });
  }

  async remove(invoice: InvoiceModel): Promise<void> {
    const confirmed = await this.confirm.ask(
      'Faturayı sil',
      `${invoice.invoiceNumber} numaralı fatura ve oluşturduğu stok hareketleri silinecek.`
    );

    if (!confirmed) return;

    this.http.post<string>('Invoices/DeleteById', { id: invoice.id }, (message) => {
      this.toast.info(message);
      this.load();
    });
  }

  private resetLineDraft(): void {
    this.lineProductId.set('');
    this.lineDepotId.set('');
    this.lineQuantity.set(null);
    this.linePrice.set(null);
  }
}

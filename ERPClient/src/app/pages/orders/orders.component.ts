import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ConfirmService } from '../../core/confirm.service';
import { MoneyPipe, QuantityPipe, TrDatePipe, toInputDate } from '../../core/format';
import { HttpService } from '../../core/http.service';
import { ToastService } from '../../core/toast.service';
import {
  CustomerModel,
  OrderDetailModel,
  OrderDraft,
  OrderModel,
  OrderStatus,
  ProductModel,
} from '../../models/domain.model';
import { IconComponent } from '../../ui/icon.component';
import { ModalComponent } from '../../ui/modal.component';
import { EmptyComponent, PageComponent, PanelComponent, SearchComponent } from '../../ui/primitives';

type StatusFilter = 'all' | '1' | '2' | '3';

@Component({
  selector: 'tz-orders',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    RouterLink,
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
  templateUrl: './orders.component.html',
})
export class OrdersComponent implements OnInit {
  private readonly http = inject(HttpService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly orders = signal<OrderModel[]>([]);
  readonly customers = signal<CustomerModel[]>([]);
  readonly products = signal<ProductModel[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly statusFilter = signal<StatusFilter>('all');
  readonly submitted = signal(false);

  readonly editing = signal<OrderDraft | null>(null);
  readonly isNew = signal(true);

  /** Kip içindeki kalem giriş satırı. */
  readonly lineProductId = signal('');
  readonly lineQuantity = signal<number | null>(null);
  readonly linePrice = signal<number | null>(null);

  readonly statuses = [
    { value: 'all' as StatusFilter, label: 'Tümü' },
    { value: '1' as StatusFilter, label: 'Bekliyor' },
    { value: '2' as StatusFilter, label: 'Planlandı' },
    { value: '3' as StatusFilter, label: 'Tamamlandı' },
  ];

  readonly filtered = computed(() => {
    const term = this.search().trim().toLocaleLowerCase('tr');
    const status = this.statusFilter();

    return this.orders().filter((order) => {
      const matchesStatus = status === 'all' || String(order.status?.value) === status;
      const matchesTerm =
        !term ||
        `${order.number} ${order.customer?.name ?? ''}`.toLocaleLowerCase('tr').includes(term);

      return matchesStatus && matchesTerm;
    });
  });

  readonly pendingCount = computed(
    () => this.orders().filter((order) => order.status?.value === OrderStatus.Pending).length
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);

    forkJoin({
      orders: this.http.request<OrderModel[]>('Orders/GetAll'),
      customers: this.http.request<CustomerModel[]>('Customers/GetAll'),
      products: this.http.request<ProductModel[]>('Products/GetAll'),
    }).subscribe({
      next: ({ orders, customers, products }) => {
        this.orders.set(orders ?? []);
        this.customers.set(customers ?? []);
        this.products.set(products ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openCreate(): void {
    const model: OrderDraft = { ...new OrderModel(), details: [] };
    model.date = toInputDate();
    model.deliveryDate = toInputDate();

    this.submitted.set(false);
    this.isNew.set(true);
    this.resetLineDraft();
    this.editing.set(model);
  }

  openEdit(order: OrderModel): void {
    this.submitted.set(false);
    this.isNew.set(false);
    this.resetLineDraft();

    // Kalemler de kopyalanıyor; kipte satır silmek listedeki siparişi bozmamalı.
    this.editing.set({
      ...order,
      date: this.asInputDate(order.date),
      deliveryDate: this.asInputDate(order.deliveryDate),
      details: (order.details ?? []).map((detail) => ({ ...detail })),
    });
  }

  close(): void {
    this.editing.set(null);
  }

  addLine(): void {
    const model = this.editing();
    const productId = this.lineProductId();
    const quantity = Number(this.lineQuantity());
    const price = Number(this.linePrice());

    if (!model) return;

    if (!productId || !(quantity > 0)) {
      this.toast.warn('Ürün ve sıfırdan büyük bir miktar seçin.');
      return;
    }

    const product = this.products().find((item) => item.id === productId);

    const line: OrderDetailModel = {
      ...new OrderDetailModel(),
      productId,
      product: product ?? new ProductModel(),
      quantity,
      price: Number.isFinite(price) ? price : 0,
    };

    this.editing.set({ ...model, details: [...model.details, line] });
    this.resetLineDraft();
  }

  removeLine(index: number): void {
    const model = this.editing();
    if (!model) return;

    this.editing.set({
      ...model,
      details: model.details.filter((_, i) => i !== index),
    });
  }

  total(order: OrderModel): number {
    return (order.details ?? []).reduce(
      (sum, detail) => sum + detail.quantity * detail.price,
      0
    );
  }

  save(form: NgForm): void {
    this.submitted.set(true);

    const model = this.editing();
    if (!model || form.invalid) return;

    if (!model.details.length) {
      this.toast.warn('Siparişe en az bir kalem ekleyin.');
      return;
    }

    const details = model.details.map((detail) => ({
      productId: detail.productId,
      quantity: detail.quantity,
      price: detail.price,
    }));

    const isNew = this.isNew();
    const body = isNew
      ? {
          customerId: model.customerId,
          date: model.date,
          deliveryDate: model.deliveryDate,
          details,
        }
      : {
          id: model.id,
          customerId: model.customerId,
          date: model.date,
          deliveryDate: model.deliveryDate,
          details,
        };

    this.http.post<string>(isNew ? 'Orders/Create' : 'Orders/Update', body, (message) => {
      this.toast.ok(message);
      this.close();
      this.load();
    });
  }

  async remove(order: OrderModel): Promise<void> {
    const confirmed = await this.confirm.ask(
      'Siparişi sil',
      `${order.number} numaralı sipariş kalemleriyle birlikte silinecek.`
    );

    if (!confirmed) return;

    this.http.post<string>('Orders/DeleteById', { id: order.id }, (message) => {
      this.toast.info(message);
      this.load();
    });
  }

  statusClass(value: number | undefined): string {
    if (value === OrderStatus.Completed) return 'tz-badge--ok';
    if (value === OrderStatus.Planned) return 'tz-badge--info';

    return 'tz-badge--warn';
  }

  private resetLineDraft(): void {
    this.lineProductId.set('');
    this.lineQuantity.set(null);
    this.linePrice.set(null);
  }

  /** Sunucu `yyyy-MM-dd` gönderiyor; tarih girdisi de aynı biçimi bekliyor. */
  private asInputDate(value: string): string {
    return value ? value.slice(0, 10) : toInputDate();
  }
}

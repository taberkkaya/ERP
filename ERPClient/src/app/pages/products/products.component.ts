import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ConfirmService } from '../../core/confirm.service';
import { QuantityPipe } from '../../core/format';
import { HttpService } from '../../core/http.service';
import { ToastService } from '../../core/toast.service';
import { ProductModel, productTypes } from '../../models/domain.model';
import { IconComponent } from '../../ui/icon.component';
import { ModalComponent } from '../../ui/modal.component';
import { EmptyComponent, PageComponent, PanelComponent, SearchComponent } from '../../ui/primitives';

type TypeFilter = 'all' | '1' | '2';

@Component({
  selector: 'tz-products',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    QuantityPipe,
    IconComponent,
    ModalComponent,
    PageComponent,
    PanelComponent,
    EmptyComponent,
    SearchComponent,
  ],
  templateUrl: './products.component.html',
})
export class ProductsComponent implements OnInit {
  private readonly http = inject(HttpService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly productTypes = productTypes;

  readonly products = signal<ProductModel[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly typeFilter = signal<TypeFilter>('all');
  readonly submitted = signal(false);
  readonly editing = signal<ProductModel | null>(null);
  readonly isNew = signal(true);

  readonly filtered = computed(() => {
    const term = this.search().trim().toLocaleLowerCase('tr');
    const type = this.typeFilter();

    return this.products().filter((product) => {
      const matchesType = type === 'all' || String(product.productType?.value) === type;
      const matchesTerm = !term || product.name.toLocaleLowerCase('tr').includes(term);

      return matchesType && matchesTerm;
    });
  });

  /** Stoğu tükenmiş kalemler üstte bir uyarı sayısı olarak gösteriliyor. */
  readonly outOfStock = computed(() => this.products().filter((p) => p.stock <= 0).length);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);

    this.http.post<ProductModel[]>(
      'Products/GetAll',
      {},
      (data) => {
        this.products.set(data ?? []);
        this.loading.set(false);
      },
      () => this.loading.set(false)
    );
  }

  openCreate(): void {
    this.submitted.set(false);
    this.isNew.set(true);
    this.editing.set(new ProductModel());
  }

  openEdit(product: ProductModel): void {
    this.submitted.set(false);
    this.isNew.set(false);
    // Sunucu türü `productTypeValue` alanından okuyor; listede gelen ise nesne.
    this.editing.set({ ...product, productTypeValue: product.productType?.value ?? 1 });
  }

  close(): void {
    this.editing.set(null);
  }

  save(form: NgForm): void {
    this.submitted.set(true);

    const model = this.editing();
    if (!model || form.invalid) return;

    this.http.post<string>(
      this.isNew() ? 'Products/Create' : 'Products/Update',
      model,
      (message) => {
        this.toast.ok(message);
        this.close();
        this.load();
      }
    );
  }

  async remove(product: ProductModel): Promise<void> {
    const confirmed = await this.confirm.ask(
      'Ürünü sil',
      `${product.name} kaydı silinecek. Ürün bir reçetede veya hareket kaydında geçiyorsa işlem reddedilir.`
    );

    if (!confirmed) return;

    this.http.post<string>('Products/DeleteById', { id: product.id }, (message) => {
      this.toast.info(message);
      this.load();
    });
  }
}

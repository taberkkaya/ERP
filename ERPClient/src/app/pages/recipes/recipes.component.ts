import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ConfirmService } from '../../core/confirm.service';
import { QuantityPipe } from '../../core/format';
import { HttpService } from '../../core/http.service';
import { ToastService } from '../../core/toast.service';
import { ProductModel, RecipeModel } from '../../models/domain.model';
import { IconComponent } from '../../ui/icon.component';
import { ModalComponent } from '../../ui/modal.component';
import { EmptyComponent, PageComponent, PanelComponent, SearchComponent } from '../../ui/primitives';

interface DraftLine {
  productId: string;
  name: string;
  quantity: number;
}

@Component({
  selector: 'tz-recipes',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    RouterLink,
    QuantityPipe,
    IconComponent,
    ModalComponent,
    PageComponent,
    PanelComponent,
    EmptyComponent,
    SearchComponent,
  ],
  templateUrl: './recipes.component.html',
})
export class RecipesComponent implements OnInit {
  private readonly http = inject(HttpService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly recipes = signal<RecipeModel[]>([]);
  readonly products = signal<ProductModel[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly creating = signal(false);
  readonly submitted = signal(false);

  /** Yeni reçetenin ana ürünü ve bileşen satırları. */
  readonly headProductId = signal('');
  readonly lines = signal<DraftLine[]>([]);
  readonly lineProductId = signal('');
  readonly lineQuantity = signal<number | null>(null);

  readonly filtered = computed(() => {
    const term = this.search().trim().toLocaleLowerCase('tr');
    if (!term) return this.recipes();

    return this.recipes().filter((recipe) =>
      (recipe.product?.name ?? '').toLocaleLowerCase('tr').includes(term)
    );
  });

  /**
   * Reçetesi olan ürün yeniden seçilemiyor: sunucu ikinci reçeteyi kabul etse de
   * hangisinin patlatılacağı belirsiz kalırdı.
   */
  readonly headOptions = computed(() => {
    const used = new Set(this.recipes().map((recipe) => recipe.productId));
    return this.products().filter((product) => !used.has(product.id));
  });

  /** Bileşen listesi ana ürünün kendisini ve zaten eklenmiş kalemleri dışlıyor. */
  readonly lineOptions = computed(() => {
    const head = this.headProductId();
    const picked = new Set(this.lines().map((line) => line.productId));

    return this.products().filter(
      (product) => product.id !== head && !picked.has(product.id)
    );
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);

    forkJoin({
      recipes: this.http.request<RecipeModel[]>('Recipes/GetAll'),
      products: this.http.request<ProductModel[]>('Products/GetAll'),
    }).subscribe({
      next: ({ recipes, products }) => {
        this.recipes.set(recipes ?? []);
        this.products.set(products ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openCreate(): void {
    this.submitted.set(false);
    this.headProductId.set('');
    this.lines.set([]);
    this.resetLineDraft();
    this.creating.set(true);
  }

  close(): void {
    this.creating.set(false);
  }

  addLine(): void {
    const productId = this.lineProductId();
    const quantity = Number(this.lineQuantity());

    if (!productId || !(quantity > 0)) {
      this.toast.warn('Bileşen ve sıfırdan büyük bir miktar seçin.');
      return;
    }

    const product = this.products().find((item) => item.id === productId);

    this.lines.update((list) => [
      ...list,
      { productId, name: product?.name ?? '', quantity },
    ]);

    this.resetLineDraft();
  }

  removeLine(index: number): void {
    this.lines.update((list) => list.filter((_, i) => i !== index));
  }

  save(form: NgForm): void {
    this.submitted.set(true);

    if (form.invalid || !this.headProductId()) return;

    if (!this.lines().length) {
      this.toast.warn('Reçeteye en az bir bileşen ekleyin.');
      return;
    }

    const body = {
      productId: this.headProductId(),
      details: this.lines().map((line) => ({
        productId: line.productId,
        quantity: line.quantity,
      })),
    };

    this.http.post<string>('Recipes/Create', body, (message) => {
      this.toast.ok(message);
      this.close();
      this.load();
    });
  }

  async remove(recipe: RecipeModel): Promise<void> {
    const confirmed = await this.confirm.ask(
      'Reçeteyi sil',
      `${recipe.product?.name} reçetesi bileşenleriyle birlikte silinecek.`
    );

    if (!confirmed) return;

    this.http.post<string>('Recipes/DeleteById', { id: recipe.id }, (message) => {
      this.toast.info(message);
      this.load();
    });
  }

  private resetLineDraft(): void {
    this.lineProductId.set('');
    this.lineQuantity.set(null);
  }
}

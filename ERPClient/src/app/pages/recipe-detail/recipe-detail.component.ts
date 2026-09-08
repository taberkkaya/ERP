import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ConfirmService } from '../../core/confirm.service';
import { QuantityPipe } from '../../core/format';
import { HttpService } from '../../core/http.service';
import { ToastService } from '../../core/toast.service';
import { ProductModel, RecipeDetailModel, RecipeModel } from '../../models/domain.model';
import { IconComponent } from '../../ui/icon.component';
import { ModalComponent } from '../../ui/modal.component';
import { EmptyComponent, PageComponent, PanelComponent } from '../../ui/primitives';

interface DetailDraft {
  id: string;
  productId: string;
  quantity: number;
}

@Component({
  selector: 'tz-recipe-detail',
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
  ],
  templateUrl: './recipe-detail.component.html',
})
export class RecipeDetailComponent implements OnInit {
  private readonly http = inject(HttpService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly recipe = signal<RecipeModel | null>(null);
  readonly products = signal<ProductModel[]>([]);
  readonly loading = signal(true);
  readonly submitted = signal(false);
  readonly editing = signal<DetailDraft | null>(null);
  readonly isNew = signal(true);

  private readonly recipeId = this.route.snapshot.paramMap.get('id') ?? '';

  /** Ana ürün ve zaten eklenmiş bileşenler seçeneklerden çıkarılıyor. */
  readonly options = computed(() => {
    const recipe = this.recipe();
    const current = this.editing();
    const used = new Set((recipe?.details ?? []).map((detail) => detail.productId));

    // Düzenlenen satırın kendi ürünü listede kalmalı, aksi hâlde seçim boşalıyor.
    if (current && !this.isNew()) used.delete(current.productId);

    return this.products().filter(
      (product) => product.id !== recipe?.productId && !used.has(product.id)
    );
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);

    forkJoin({
      recipe: this.http.request<RecipeModel>('RecipeDetails/GetRecipeByIdWithDetails', {
        recipeId: this.recipeId,
      }),
      products: this.http.request<ProductModel[]>('Products/GetAll'),
    }).subscribe({
      next: ({ recipe, products }) => {
        this.recipe.set(recipe ?? null);
        this.products.set(products ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  openCreate(): void {
    this.submitted.set(false);
    this.isNew.set(true);
    this.editing.set({ id: '', productId: '', quantity: 1 });
  }

  openEdit(detail: RecipeDetailModel): void {
    this.submitted.set(false);
    this.isNew.set(false);
    this.editing.set({
      id: detail.id,
      productId: detail.productId,
      quantity: detail.quantity,
    });
  }

  close(): void {
    this.editing.set(null);
  }

  save(form: NgForm): void {
    this.submitted.set(true);

    const draft = this.editing();
    if (!draft || form.invalid) return;

    if (!(draft.quantity > 0)) {
      this.toast.warn('Miktar sıfırdan büyük olmalı.');
      return;
    }

    const isNew = this.isNew();
    const body = isNew
      ? { recipeId: this.recipeId, productId: draft.productId, quantity: draft.quantity }
      : { id: draft.id, productId: draft.productId, quantity: draft.quantity };

    this.http.post<string>(
      isNew ? 'RecipeDetails/Create' : 'RecipeDetails/Update',
      body,
      (message) => {
        this.toast.ok(message);
        this.close();
        this.load();
      }
    );
  }

  async remove(detail: RecipeDetailModel): Promise<void> {
    const confirmed = await this.confirm.ask(
      'Bileşeni kaldır',
      `${detail.product?.name} bu reçeteden çıkarılacak.`,
      { confirmLabel: 'Kaldır' }
    );

    if (!confirmed) return;

    this.http.post<string>('RecipeDetails/DeleteById', { id: detail.id }, (message) => {
      this.toast.info(message);
      this.load();
    });
  }
}

import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ConfirmService } from '../../core/confirm.service';
import { QuantityPipe, TrDatePipe } from '../../core/format';
import { HttpService } from '../../core/http.service';
import { ToastService } from '../../core/toast.service';
import {
  DepotModel,
  ProductModel,
  ProductionModel,
  RecipeModel,
} from '../../models/domain.model';
import { IconComponent } from '../../ui/icon.component';
import { ModalComponent } from '../../ui/modal.component';
import { EmptyComponent, PageComponent, PanelComponent, SearchComponent } from '../../ui/primitives';

interface ProductionDraft {
  productId: string;
  depotId: string;
  quantity: number | null;
}

@Component({
  selector: 'tz-productions',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    RouterLink,
    QuantityPipe,
    TrDatePipe,
    IconComponent,
    ModalComponent,
    PageComponent,
    PanelComponent,
    EmptyComponent,
    SearchComponent,
  ],
  templateUrl: './productions.component.html',
})
export class ProductionsComponent implements OnInit {
  private readonly http = inject(HttpService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly productions = signal<ProductionModel[]>([]);
  readonly depots = signal<DepotModel[]>([]);
  readonly recipes = signal<RecipeModel[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly submitted = signal(false);
  readonly draft = signal<ProductionDraft | null>(null);

  /**
   * Yalnızca reçetesi olan ürünler üretilebiliyor. Reçetesiz bir ürün için sunucu
   * kayıt açıyor ama hiçbir hammadde düşmüyor; bu, stoğu sessizce şişiriyordu.
   */
  readonly producible = computed<ProductModel[]>(() =>
    this.recipes()
      .map((recipe) => recipe.product)
      .filter((product): product is ProductModel => !!product)
  );

  readonly filtered = computed(() => {
    const term = this.search().trim().toLocaleLowerCase('tr');
    if (!term) return this.productions();

    return this.productions().filter((production) =>
      `${production.product?.name ?? ''} ${production.depot?.name ?? ''}`
        .toLocaleLowerCase('tr')
        .includes(term)
    );
  });

  readonly totalProduced = computed(() =>
    this.productions().reduce((sum, production) => sum + production.quantity, 0)
  );

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);

    forkJoin({
      productions: this.http.request<ProductionModel[]>('Productions/GetAll'),
      depots: this.http.request<DepotModel[]>('Depots/GetAll'),
      recipes: this.http.request<RecipeModel[]>('Recipes/GetAll'),
    }).subscribe({
      next: ({ productions, depots, recipes }) => {
        this.productions.set(productions ?? []);
        this.depots.set(depots ?? []);
        this.recipes.set(recipes ?? []);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  open(): void {
    this.submitted.set(false);
    this.draft.set({ productId: '', depotId: '', quantity: null });
  }

  close(): void {
    this.draft.set(null);
  }

  save(form: NgForm): void {
    this.submitted.set(true);

    const draft = this.draft();
    if (!draft || form.invalid) return;

    const quantity = Number(draft.quantity);

    if (!(quantity > 0)) {
      this.toast.warn('Üretim miktarı sıfırdan büyük olmalı.');
      return;
    }

    this.http.post<string>(
      'Productions/Create',
      { productId: draft.productId, depotId: draft.depotId, quantity },
      (message) => {
        this.toast.ok(message);
        this.close();
        this.load();
      }
    );
  }

  async remove(production: ProductionModel): Promise<void> {
    const confirmed = await this.confirm.ask(
      'Üretim kaydını sil',
      `${production.product?.name} üretimi ve buna bağlı stok hareketleri silinecek.`
    );

    if (!confirmed) return;

    this.http.post<string>('Productions/DeleteById', { id: production.id }, (message) => {
      this.toast.info(message);
      this.load();
    });
  }
}

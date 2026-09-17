import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { MoneyPipe, QuantityPipe, TrDatePipe } from '../../core/format';
import { HttpService } from '../../core/http.service';
import {
  OrderModel,
  OrderStatus,
  ProductModel,
  ProductionModel,
  StockSummaryModel,
} from '../../models/domain.model';
import { IconComponent } from '../../ui/icon.component';
import { EmptyComponent, PageComponent, PanelComponent } from '../../ui/primitives';

@Component({
  selector: 'tz-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    MoneyPipe,
    QuantityPipe,
    TrDatePipe,
    IconComponent,
    PageComponent,
    PanelComponent,
    EmptyComponent,
  ],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  private readonly http = inject(HttpService);

  readonly loading = signal(true);
  readonly orders = signal<OrderModel[]>([]);
  readonly products = signal<ProductModel[]>([]);
  readonly productions = signal<ProductionModel[]>([]);
  readonly stock = signal<StockSummaryModel | null>(null);

  readonly openOrders = computed(() =>
    this.orders().filter((order) => order.status?.value !== OrderStatus.Completed)
  );

  readonly pendingOrders = computed(() =>
    this.orders().filter((order) => order.status?.value === OrderStatus.Pending)
  );

  readonly openOrderValue = computed(() =>
    this.openOrders().reduce((sum, order) => sum + this.orderTotal(order), 0)
  );

  readonly outOfStock = computed(() => this.products().filter((product) => product.stock <= 0));

  /** Stoğu en düşük kalemler; sıfır olanlar en üstte. */
  readonly stockWatch = computed(() =>
    [...this.products()].sort((a, b) => a.stock - b.stock).slice(0, 6)
  );

  /** Teslim tarihi en yakın açık siparişler. */
  readonly upcoming = computed(() =>
    [...this.openOrders()]
      .sort((a, b) => (a.deliveryDate ?? '').localeCompare(b.deliveryDate ?? ''))
      .slice(0, 6)
  );

  readonly recentProductions = computed(() =>
    [...this.productions()]
      .sort((a, b) => (b.createdAt ?? '').localeCompare(a.createdAt ?? ''))
      .slice(0, 5)
  );

  readonly entryValue = computed(() => this.stock()?.entryValue ?? 0);

  readonly exitValue = computed(() => this.stock()?.exitValue ?? 0);

  /** Depoda duran malın değeri: giren eksi çıkan. */
  readonly stockValue = computed(() => this.entryValue() - this.exitValue());

  /** Hareket doğurmayan kaynaklar panelde yer kaplamasın. */
  readonly stockSources = computed(() =>
    (this.stock()?.bySource ?? []).filter((line) => line.movementCount > 0)
  );

  ngOnInit(): void {
    forkJoin({
      orders: this.http.request<OrderModel[]>('Orders/GetAll'),
      products: this.http.request<ProductModel[]>('Products/GetAll'),
      productions: this.http.request<ProductionModel[]>('Productions/GetAll'),
      stock: this.http.request<StockSummaryModel>('StockMovements/GetSummary'),
    }).subscribe({
      next: ({ orders, products, productions, stock }) => {
        this.orders.set(orders ?? []);
        this.products.set(products ?? []);
        this.productions.set(productions ?? []);
        this.stock.set(stock ?? null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  orderTotal(order: OrderModel): number {
    return (order.details ?? []).reduce(
      (sum, detail) => sum + detail.quantity * detail.price,
      0
    );
  }

  statusClass(value: number | undefined): string {
    if (value === OrderStatus.Completed) return 'tz-badge--ok';
    if (value === OrderStatus.Planned) return 'tz-badge--info';

    return 'tz-badge--warn';
  }

  /** Teslim tarihi geçmiş açık siparişler kırmızı okunuyor. */
  isLate(order: OrderModel): boolean {
    if (!order.deliveryDate) return false;

    return order.deliveryDate.slice(0, 10) < new Date().toISOString().slice(0, 10);
  }
}

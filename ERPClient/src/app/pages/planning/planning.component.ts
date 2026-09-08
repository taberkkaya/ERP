import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { QuantityPipe, TrDatePipe } from '../../core/format';
import { HttpService } from '../../core/http.service';
import { RequirementsPlanningModel } from '../../models/domain.model';
import { IconComponent } from '../../ui/icon.component';
import { EmptyComponent, PageComponent, PanelComponent } from '../../ui/primitives';

/**
 * İhtiyaç planlaması. Sunucu siparişin kalemlerini gezip stoğu yetmeyenleri bulur,
 * bunların reçetelerini patlatır ve eksik kalan bileşenleri toplar; aynı çağrıda
 * siparişin durumunu "planlandı"ya çeker.
 */
@Component({
  selector: 'tz-planning',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    QuantityPipe,
    TrDatePipe,
    IconComponent,
    PageComponent,
    PanelComponent,
    EmptyComponent,
  ],
  templateUrl: './planning.component.html',
})
export class PlanningComponent implements OnInit {
  private readonly http = inject(HttpService);
  private readonly route = inject(ActivatedRoute);

  readonly plan = signal<RequirementsPlanningModel | null>(null);
  readonly loading = signal(true);

  ngOnInit(): void {
    const orderId = this.route.snapshot.paramMap.get('orderId');

    this.http.post<RequirementsPlanningModel>(
      'Orders/RequirementsPlanningByOrderId',
      { orderId },
      (data) => {
        this.plan.set(data ?? null);
        this.loading.set(false);
      },
      () => this.loading.set(false)
    );
  }

  print(): void {
    window.print();
  }
}

import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DemoService } from '../core/demo.service';
import { ToastService } from '../core/toast.service';
import { formatCountdown } from '../core/format';
import { IconComponent } from '../ui/icon.component';

/**
 * Demo şeridi: ziyaretçiye kaç işlem hakkı ve ne kadar süre kaldığını söyler,
 * sandbox'ı sıfırlama ve iletişim bağlantısını taşır.
 */
@Component({
  selector: 'tz-demo-bar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IconComponent],
  template: `
    @if (demo.status(); as status) {
      <div class="tz-demobar">
        <span class="tz-demobar__tag">Demo</span>

        <span class="tz-demobar__stat tz-demobar__stat--wide">
          {{ status.workspaceName }}
        </span>

        <span class="tz-demobar__stat">
          <tz-icon name="spark" style="width: 14px; height: 14px" />
          <b>{{ demo.writesLeft() }}</b> işlem hakkı
        </span>

        <span class="tz-demobar__meter">
          <span class="tz-meter">
            <span
              class="tz-meter__fill"
              [class.tz-meter__fill--warn]="demo.writeRatio() >= 60"
              [class.tz-meter__fill--danger]="demo.writeRatio() >= 85"
              [style.width.%]="demo.writeRatio()"
            ></span>
          </span>
        </span>

        <span class="tz-demobar__stat">
          <tz-icon name="clock" style="width: 14px; height: 14px" />
          <b>{{ countdown() }}</b>
        </span>

        <span class="tz-demobar__tools">
          <button
            type="button"
            class="tz-btn tz-btn--sm"
            [disabled]="demo.starting()"
            (click)="reset()"
          >
            <tz-icon name="refresh" />
            Sıfırla
          </button>
          <button type="button" class="tz-btn tz-btn--sm" (click)="demo.openContactPage()">
            <tz-icon name="external" />
            İletişim
          </button>
        </span>
      </div>
    }
  `,
})
export class DemoBarComponent {
  readonly demo = inject(DemoService);
  private readonly toast = inject(ToastService);

  readonly countdown = computed(() => formatCountdown(this.demo.secondsRemaining()));

  reset(): void {
    this.demo.reset().subscribe({
      next: () => {
        this.toast.ok('Demo verileri başlangıç durumuna döndürüldü.');
        // Ekranlar veriyi açılışta çekiyor; sandbox değiştiği için sayfa
        // yeniden yüklenmeden eski kayıtlar ekranda kalırdı.
        window.location.reload();
      },
      error: () => this.toast.error('Demo sıfırlanamadı, birazdan tekrar deneyin.'),
    });
  }
}

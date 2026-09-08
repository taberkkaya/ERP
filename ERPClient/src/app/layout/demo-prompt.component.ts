import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DemoService } from '../core/demo.service';
import { ToastService } from '../core/toast.service';
import { IconComponent } from '../ui/icon.component';
import { ModalComponent } from '../ui/modal.component';

/**
 * İki durumda açılır: ziyaretçi kotasının ortasına geldiğinde bir davet
 * ("nudge"), oturum bittiğinde ise kapanışı bildiren pencere. İkincisinde
 * kapatma yok — devam etmek için yeni bir oturum açılması gerekiyor.
 */
@Component({
  selector: 'tz-demo-prompt',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ModalComponent, IconComponent],
  template: `
    @if (demo.prompt(); as kind) {
      <tz-modal
        slim
        [title]="kind === 'ended' ? 'Demo oturumu bitti' : 'Nasıl gidiyor?'"
        (close)="onClose(kind)"
      >
        <div class="tz-demoprompt__mark">
          <tz-icon [name]="kind === 'ended' ? 'clock' : 'spark'" />
        </div>

        @if (kind === 'ended') {
          <div class="tz-demoprompt__title">Bu oturum sona erdi</div>
          <p class="tz-demoprompt__text">
            Demo alanı bir sonraki ziyaretçi için sıfırlandı. Yeni bir oturum açıp
            baştan deneyebilir ya da projeyi konuşmak için iletişime geçebilirsiniz.
          </p>
        } @else {
          <div class="tz-demoprompt__title">Ayırdığımız işlem hakkının yarısını geçtiniz</div>
          <p class="tz-demoprompt__text">
            Demo alanı sınırlı: kısa bir süre ve sayılı işlem içeriyor. Uygulamayı
            kendi verinizle görmek isterseniz iletişime geçebilirsiniz.
          </p>
        }

        <ng-container modalFooter>
          @if (kind === 'ended') {
            <button type="button" class="tz-btn" (click)="restart()" [disabled]="demo.starting()">
              <tz-icon name="refresh" />
              Yeni oturum
            </button>
          } @else {
            <button type="button" class="tz-btn" (click)="demo.dismissPrompt()">
              Denemeye devam
            </button>
          }
          <button type="button" class="tz-btn tz-btn--primary" (click)="demo.openContactPage()">
            <tz-icon name="external" />
            İletişime geç
          </button>
        </ng-container>
      </tz-modal>
    }
  `,
})
export class DemoPromptComponent {
  readonly demo = inject(DemoService);
  private readonly toast = inject(ToastService);

  /** Biten oturum kapatılamaz: arkasındaki ekran zaten çalışmıyor. */
  onClose(kind: string): void {
    if (kind !== 'ended') this.demo.dismissPrompt();
  }

  restart(): void {
    this.demo.reset().subscribe({
      next: () => window.location.reload(),
      error: () => this.toast.error('Şu anda boş demo alanı yok, birazdan tekrar deneyin.'),
    });
  }
}

import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { DemoService } from '../../core/demo.service';
import { HttpService } from '../../core/http.service';
import { LoginModel, LoginResponseModel } from '../../models/auth.model';
import { IconComponent } from '../../ui/icon.component';
import { ToastHostComponent } from '../../ui/toast-host.component';

interface FlowStep {
  name: string;
  text: string;
}

/**
 * Giriş kapısı. İki yol sunuyor: tek tıkla demo oturumu ve gerçek kullanıcı girişi.
 * Demo kapalı bir kurulumda ilk yol hiç görünmüyor.
 */
@Component({
  selector: 'tz-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, IconComponent, ToastHostComponent],
  templateUrl: './login.component.html',
})
export class LoginComponent implements OnInit {
  private readonly http = inject(HttpService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly demo = inject(DemoService);

  readonly model = new LoginModel();
  readonly busy = signal(false);
  readonly error = signal('');
  readonly demoEnabled = signal(false);
  readonly submitted = signal(false);

  readonly steps: FlowStep[] = [
    {
      name: 'Sipariş',
      text: 'Müşteri siparişini kalemleriyle açın; belge numarası ve teslim tarihi otomatik yürür.',
    },
    {
      name: 'İhtiyaç planı',
      text: 'Reçete patlatılır, stok düşülür, üretilecek ve satın alınacak kalemler listelenir.',
    },
    {
      name: 'Üretim',
      text: 'Üretim kaydı reçetedeki hammaddeyi depodan düşer, mamulü stoğa alır.',
    },
    {
      name: 'Fatura',
      text: 'Alış faturası stoğa girer, satış faturası çıkarır; sipariş tamamlandıya döner.',
    },
  ];

  ngOnInit(): void {
    // Demo kapalıysa düğmeyi hiç göstermiyoruz: basıldığında 404 dönerdi.
    this.demo.config().subscribe({
      next: (result) => this.demoEnabled.set(result.data?.enabled ?? false),
      error: () => this.demoEnabled.set(false),
    });
  }

  startDemo(): void {
    this.error.set('');

    this.demo.start().subscribe({
      next: (result) => {
        if (result.data) this.router.navigateByUrl('/');
      },
      error: (err) =>
        this.error.set(
          err?.error?.errorMessages?.[0] ??
            'Şu anda boş demo alanı yok. Birkaç dakika sonra tekrar deneyin.'
        ),
    });
  }

  signIn(form: NgForm): void {
    this.submitted.set(true);

    if (form.invalid) return;

    this.busy.set(true);
    this.error.set('');

    this.http.post<LoginResponseModel>(
      'Auth/Login',
      this.model,
      (response) => {
        this.busy.set(false);
        this.auth.store(response.token);
        this.router.navigateByUrl('/');
      },
      () => this.busy.set(false)
    );
  }
}

import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { DemoService } from '../../core/demo.service';
import { HttpService } from '../../core/http.service';
import { LoginModel, LoginResponseModel } from '../../models/auth.model';
import { IconComponent } from '../../ui/icon.component';
import { ModalComponent } from '../../ui/modal.component';
import { ToastHostComponent } from '../../ui/toast-host.component';

interface FlowStep {
  name: string;
  text: string;
}

/** Demoya giriş akışının hangi adımında olduğumuz. */
type DemoStep = 'email' | 'code';

/**
 * Giriş kapısı. İki yol sunuyor: tek tıkla demo oturumu ve gerçek kullanıcı girişi.
 * Demo kapalı bir kurulumda ilk yol hiç görünmüyor.
 */
@Component({
  selector: 'tz-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, IconComponent, ModalComponent, ToastHostComponent],
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

  /** Sunucu doğrulama istemiyorsa (mail yapılandırılmamışsa) akış tek tıka iner. */
  readonly demoNeedsVerification = signal(false);

  readonly demoOpen = signal(false);
  readonly demoStep = signal<DemoStep>('email');
  readonly demoBusy = signal(false);
  readonly demoNote = signal('');
  readonly demoError = signal('');

  demoEmail = '';
  demoCode = '';

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
      name: 'Stok',
      text: 'Her giriş ve çıkış harekete yazılır; eldeki miktar hep bu hareketlerden hesaplanır.',
    },
  ];

  ngOnInit(): void {
    // Daha önce doğrulanmış adres varsa alan dolu açılsın.
    this.demoEmail = this.demo.rememberedEmail;

    // Demo kapalıysa düğmeyi hiç göstermiyoruz: basıldığında 404 dönerdi.
    this.demo.config().subscribe({
      next: (result) => {
        this.demoEnabled.set(result.data?.enabled ?? false);
        this.demoNeedsVerification.set(result.data?.emailVerificationRequired ?? false);
      },
      error: () => {
        this.demoEnabled.set(false);
        this.demoNeedsVerification.set(false);
      },
    });
  }

  startDemo(): void {
    this.error.set('');

    if (!this.demoNeedsVerification()) {
      this.runDemoStart();
      return;
    }

    const remembered = this.demo.rememberedEmail;

    // Adresini daha önce doğrulamış ziyaretçi kod turuna hiç girmeden geçer.
    // Sunucudaki doğrulama penceresi kapanmışsa istek reddedilir ve aşağıdaki
    // yedek e-posta adımı açılır; bu bir hata değil, normal akış.
    if (remembered) {
      this.demoEmail = remembered;
      this.runDemoStart(remembered, '', () => this.openDemoDialog());
      return;
    }

    this.openDemoDialog();
  }

  closeDemoDialog(): void {
    this.demoOpen.set(false);
  }

  /** Kodu isteyip ikinci adıma geçer; adres zaten doğrulanmışsa doğrudan başlatır. */
  sendDemoCode(): void {
    if (!this.demoEmail.trim()) {
      this.demoError.set('E-posta adresinizi yazın.');
      return;
    }

    this.demoBusy.set(true);
    this.demoError.set('');

    this.demo.requestCode(this.demoEmail).subscribe({
      next: (result) => {
        this.demoBusy.set(false);

        // Adres zaten doğrulanmışsa sunucu kod göndermedi; kod adımı atlanır.
        if (result.data?.alreadyVerified) {
          this.runDemoStart(this.demoEmail, '');
          return;
        }

        this.demoNote.set(result.data?.message ?? '');
        this.demoStep.set('code');
      },
      error: (err) => {
        this.demoBusy.set(false);
        this.demoError.set(this.readDemoError(err));
      },
    });
  }

  verifyAndStart(): void {
    if (!this.demoCode.trim()) {
      this.demoError.set('Mailinize gelen kodu yazın.');
      return;
    }

    this.runDemoStart(this.demoEmail, this.demoCode);
  }

  backToEmail(): void {
    this.demoStep.set('email');
    this.demoCode = '';
    this.demoNote.set('');
    this.demoError.set('');
  }

  /** Saklanan adresi unutur; ziyaretçi başka bir adresle baştan doğrulanır. */
  useAnotherEmail(): void {
    this.demo.forgetEmail();
    this.demoEmail = '';
    this.backToEmail();
  }

  private openDemoDialog(): void {
    this.demoStep.set('email');
    this.demoCode = '';
    this.demoNote.set('');
    this.demoError.set('');
    this.demoOpen.set(true);
  }

  /**
   * onFailure verilirse hata ziyaretçiye gösterilmez. Kodsuz denemenin reddedilmesi
   * beklenen bir durum: doğrulama penceresi kapanmış olabilir. Bunun karşılığı bir
   * uyarı değil, sessizce e-posta adımına düşmek.
   */
  private runDemoStart(email = '', code = '', onFailure?: () => void): void {
    this.demoBusy.set(true);

    this.demo.start(email, code).subscribe({
      next: (result) => {
        this.demoBusy.set(false);

        if (result.data) {
          this.demoOpen.set(false);
          this.router.navigateByUrl('/');
        }
      },
      error: (err) => {
        this.demoBusy.set(false);

        if (onFailure) {
          onFailure();
          return;
        }

        const message = this.readDemoError(err);

        // Kip açıkken hatayı orada, kapalıyken giriş ekranında gösteriyoruz.
        if (this.demoOpen()) this.demoError.set(message);
        else this.error.set(message);
      },
    });
  }

  /** Demo açılmadığında sebebi söyler; sunucunun kendi açıklaması daha isabetli. */
  private readDemoError(err: { status?: number; error?: { errorMessages?: string[] } }): string {
    if (err?.status === 0)
      return 'Sunucuya ulaşılamadı. Bağlantınızı kontrol edip tekrar deneyin.';

    if (err?.status === 404) return 'Demo şu anda kapalı.';

    if (err?.status === 429)
      return 'Çok fazla kod istediniz. Bir süre sonra tekrar deneyin.';

    const messages = err?.error?.errorMessages;
    if (messages?.length) return messages.join(' ');

    return 'Şu anda boş demo alanı yok. Birkaç dakika sonra tekrar deneyin.';
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

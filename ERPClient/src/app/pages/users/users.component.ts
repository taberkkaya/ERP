import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ConfirmService } from '../../core/confirm.service';
import { HttpService } from '../../core/http.service';
import { ToastService } from '../../core/toast.service';
import { AppUserModel } from '../../models/auth.model';
import { IconComponent } from '../../ui/icon.component';
import { ModalComponent } from '../../ui/modal.component';
import { EmptyComponent, PageComponent, PanelComponent, SearchComponent } from '../../ui/primitives';

interface UserDraft {
  id: string;
  firstName: string;
  lastName: string;
  userName: string;
  email: string;
  password: string;
  isAdmin: boolean;
}

@Component({
  selector: 'tz-users',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    IconComponent,
    ModalComponent,
    PageComponent,
    PanelComponent,
    EmptyComponent,
    SearchComponent,
  ],
  templateUrl: './users.component.html',
})
export class UsersComponent implements OnInit {
  private readonly http = inject(HttpService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly users = signal<AppUserModel[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly submitted = signal(false);

  readonly editing = signal<UserDraft | null>(null);
  readonly isNew = signal(true);

  /** Parola sıfırlama ayrı bir pencerede; hesap bilgileriyle karışmasın. */
  readonly resetting = signal<AppUserModel | null>(null);
  readonly newPassword = signal('');

  readonly filtered = computed(() => {
    const term = this.search().trim().toLocaleLowerCase('tr');
    if (!term) return this.users();

    return this.users().filter((user) =>
      `${user.fullName} ${user.userName} ${user.email}`.toLocaleLowerCase('tr').includes(term)
    );
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);

    this.http.post<AppUserModel[]>(
      'Users/GetAll',
      {},
      (data) => {
        this.users.set(data ?? []);
        this.loading.set(false);
      },
      () => this.loading.set(false)
    );
  }

  openCreate(): void {
    this.submitted.set(false);
    this.isNew.set(true);
    this.editing.set({
      id: '',
      firstName: '',
      lastName: '',
      userName: '',
      email: '',
      password: '',
      // Yeni hesap varsayılan olarak yönetici değil; yetki bilinçli verilmeli.
      isAdmin: false,
    });
  }

  openEdit(user: AppUserModel): void {
    this.submitted.set(false);
    this.isNew.set(false);
    this.editing.set({
      id: user.id,
      firstName: user.firstName,
      lastName: user.lastName,
      userName: user.userName,
      email: user.email,
      password: '',
      isAdmin: user.isAdmin,
    });
  }

  close(): void {
    this.editing.set(null);
  }

  save(form: NgForm): void {
    this.submitted.set(true);

    const draft = this.editing();
    if (!draft || form.invalid) return;

    const isNew = this.isNew();

    // Parola yalnızca oluştururken gönderiliyor; değiştirmek ayrı bir uç.
    const body = isNew
      ? {
          firstName: draft.firstName,
          lastName: draft.lastName,
          userName: draft.userName,
          email: draft.email,
          password: draft.password,
          isAdmin: draft.isAdmin,
        }
      : {
          id: draft.id,
          firstName: draft.firstName,
          lastName: draft.lastName,
          userName: draft.userName,
          email: draft.email,
          isAdmin: draft.isAdmin,
        };

    this.http.post<string>(isNew ? 'Users/Create' : 'Users/Update', body, (message) => {
      this.toast.ok(message);
      this.close();
      this.load();
    });
  }

  openReset(user: AppUserModel): void {
    this.submitted.set(false);
    this.newPassword.set('');
    this.resetting.set(user);
  }

  closeReset(): void {
    this.resetting.set(null);
  }

  savePassword(form: NgForm): void {
    this.submitted.set(true);

    const user = this.resetting();
    if (!user || form.invalid) return;

    this.http.post<string>(
      'Users/ChangePassword',
      { id: user.id, password: this.newPassword() },
      (message) => {
        this.toast.ok(message);
        this.closeReset();
      }
    );
  }

  async remove(user: AppUserModel): Promise<void> {
    const confirmed = await this.confirm.ask(
      'Kullanıcıyı sil',
      `${user.fullName} (${user.userName}) hesabı silinecek. Bu işlem geri alınamaz.`
    );

    if (!confirmed) return;

    this.http.post<string>('Users/DeleteById', { id: user.id }, (message) => {
      this.toast.info(message);
      this.load();
    });
  }

  /** Listedeki avatar rozetinin harfleri. */
  initials(user: AppUserModel): string {
    const first = user.firstName?.charAt(0) ?? '';
    const last = user.lastName?.charAt(0) ?? '';

    return `${first}${last}`.toLocaleUpperCase('tr') || '?';
  }
}

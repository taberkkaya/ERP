import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ConfirmService } from '../../core/confirm.service';
import { HttpService } from '../../core/http.service';
import { ToastService } from '../../core/toast.service';
import { CustomerModel } from '../../models/domain.model';
import { IconComponent } from '../../ui/icon.component';
import { ModalComponent } from '../../ui/modal.component';
import { EmptyComponent, PageComponent, PanelComponent, SearchComponent } from '../../ui/primitives';

@Component({
  selector: 'tz-customers',
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
  templateUrl: './customers.component.html',
})
export class CustomersComponent implements OnInit {
  private readonly http = inject(HttpService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly customers = signal<CustomerModel[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly submitted = signal(false);

  /** null iken kip kapalı; yeni kayıt ve düzenleme aynı formu paylaşıyor. */
  readonly editing = signal<CustomerModel | null>(null);
  readonly isNew = signal(true);

  readonly filtered = computed(() => {
    const term = this.search().trim().toLocaleLowerCase('tr');
    const list = this.customers();

    if (!term) return list;

    return list.filter((customer) =>
      [customer.name, customer.city, customer.town, customer.taxNumber, customer.taxDepartment]
        .join(' ')
        .toLocaleLowerCase('tr')
        .includes(term)
    );
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);

    this.http.post<CustomerModel[]>(
      'Customers/GetAll',
      {},
      (data) => {
        this.customers.set(data ?? []);
        this.loading.set(false);
      },
      () => this.loading.set(false)
    );
  }

  openCreate(): void {
    this.submitted.set(false);
    this.isNew.set(true);
    this.editing.set(new CustomerModel());
  }

  openEdit(customer: CustomerModel): void {
    this.submitted.set(false);
    this.isNew.set(false);
    // Kopya üzerinde çalışılıyor: vazgeçildiğinde tablodaki satır değişmemeli.
    this.editing.set({ ...customer });
  }

  close(): void {
    this.editing.set(null);
  }

  save(form: NgForm): void {
    this.submitted.set(true);

    const model = this.editing();
    if (!model || form.invalid) return;

    const isNew = this.isNew();

    this.http.post<string>(isNew ? 'Customers/Create' : 'Customers/Update', model, (message) => {
      this.toast.ok(message);
      this.close();
      this.load();
    });
  }

  async remove(customer: CustomerModel): Promise<void> {
    const confirmed = await this.confirm.ask(
      'Müşteriyi sil',
      `${customer.name} kaydı silinecek. Bu işlem geri alınamaz.`
    );

    if (!confirmed) return;

    this.http.post<string>('Customers/DeleteById', { id: customer.id }, (message) => {
      this.toast.info(message);
      this.load();
    });
  }
}

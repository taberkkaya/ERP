import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ConfirmService } from '../../core/confirm.service';
import { HttpService } from '../../core/http.service';
import { ToastService } from '../../core/toast.service';
import { DepotModel } from '../../models/domain.model';
import { IconComponent } from '../../ui/icon.component';
import { ModalComponent } from '../../ui/modal.component';
import { EmptyComponent, PageComponent, PanelComponent, SearchComponent } from '../../ui/primitives';

@Component({
  selector: 'tz-depots',
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
  templateUrl: './depots.component.html',
})
export class DepotsComponent implements OnInit {
  private readonly http = inject(HttpService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly depots = signal<DepotModel[]>([]);
  readonly loading = signal(true);
  readonly search = signal('');
  readonly submitted = signal(false);
  readonly editing = signal<DepotModel | null>(null);
  readonly isNew = signal(true);

  readonly filtered = computed(() => {
    const term = this.search().trim().toLocaleLowerCase('tr');
    const list = this.depots();

    if (!term) return list;

    return list.filter((depot) =>
      [depot.name, depot.city, depot.town, depot.address]
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

    this.http.post<DepotModel[]>(
      'Depots/GetAll',
      {},
      (data) => {
        this.depots.set(data ?? []);
        this.loading.set(false);
      },
      () => this.loading.set(false)
    );
  }

  openCreate(): void {
    this.submitted.set(false);
    this.isNew.set(true);
    this.editing.set(new DepotModel());
  }

  openEdit(depot: DepotModel): void {
    this.submitted.set(false);
    this.isNew.set(false);
    this.editing.set({ ...depot });
  }

  close(): void {
    this.editing.set(null);
  }

  save(form: NgForm): void {
    this.submitted.set(true);

    const model = this.editing();
    if (!model || form.invalid) return;

    this.http.post<string>(
      this.isNew() ? 'Depots/Create' : 'Depots/Update',
      model,
      (message) => {
        this.toast.ok(message);
        this.close();
        this.load();
      }
    );
  }

  async remove(depot: DepotModel): Promise<void> {
    const confirmed = await this.confirm.ask(
      'Depoyu sil',
      `${depot.name} kaydı silinecek. Depoya bağlı stok hareketi varsa işlem reddedilir.`
    );

    if (!confirmed) return;

    this.http.post<string>('Depots/DeleteById', { id: depot.id }, (message) => {
      this.toast.info(message);
      this.load();
    });
  }
}

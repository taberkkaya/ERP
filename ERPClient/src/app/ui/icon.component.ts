import { ChangeDetectionStrategy, Component, Input, computed, signal } from '@angular/core';

/**
 * Tezgah ikon takımı.
 *
 * Tamamı tek bir çizgi kalınlığında, 24 birimlik ızgarada çizilmiş yol verisi.
 * Font Awesome gibi bir kitaplık yerine bunun tercih edilmesinin sebebi: takım
 * otuz küçük yol dizesi kadar yer tutuyor, ikonlar `currentColor` ile temaya
 * uyuyor ve yayına ek bir yazı tipi indirmesi binmiyor.
 */
const ICONS: Record<string, string[]> = {
  /* --- marka --- */
  brand: ['M3.5 15.5h17', 'M7.5 15.5v5', 'M16.5 15.5v5', 'M12 3.5 17 8.5 12 13.5 7 8.5z'],

  /* --- gezinme --- */
  dashboard: ['M20.5 15.5a9 9 0 1 0-17 0', 'M12 15.5 16 11'],
  customers: [
    'M16 20.5v-1.5a4 4 0 0 0-4-4H7.5a4 4 0 0 0-4 4v1.5',
    'M13.25 7.25a3.5 3.5 0 1 1-7 0 3.5 3.5 0 0 1 7 0z',
    'M20.5 20.5v-1.5a4 4 0 0 0-3-3.87',
    'M15.75 3.85a3.5 3.5 0 0 1 0 6.8',
  ],
  depots: ['M3 21V9.5L12 5l9 4.5V21', 'M2.5 21h19', 'M7.5 21v-6.5h9V21', 'M7.5 17.5h9'],
  products: ['M12 3l8 4.5v9L12 21l-8-4.5v-9L12 3z', 'M4 7.5l8 4.5 8-4.5', 'M12 12v9'],
  recipes: ['M12 3l9 5-9 5-9-5 9-5z', 'M3 12.5 12 17.5l9-5', 'M3 16.5 12 21.5l9-5'],
  orders: [
    'M9 4.5H7.5A1.5 1.5 0 0 0 6 6v13.5A1.5 1.5 0 0 0 7.5 21h9a1.5 1.5 0 0 0 1.5-1.5V6a1.5 1.5 0 0 0-1.5-1.5H15',
    'M9 3h6v3H9z',
    'M9.5 11h5',
    'M9.5 15h5',
  ],
  'invoice-in': [
    'M13.5 3H7.5A1.5 1.5 0 0 0 6 4.5v15A1.5 1.5 0 0 0 7.5 21h9a1.5 1.5 0 0 0 1.5-1.5V7.5L13.5 3z',
    'M13.5 3v4.5H18',
    'M12 10.5v6',
    'M9.5 14 12 16.5 14.5 14',
  ],
  'invoice-out': [
    'M13.5 3H7.5A1.5 1.5 0 0 0 6 4.5v15A1.5 1.5 0 0 0 7.5 21h9a1.5 1.5 0 0 0 1.5-1.5V7.5L13.5 3z',
    'M13.5 3v4.5H18',
    'M12 16.5v-6',
    'M9.5 13 12 10.5 14.5 13',
  ],
  production: ['M2.5 21h19', 'M4 21V11l5 3V11l5 3V11l5 3v7', 'M16.5 8V3h3.5v5'],
  planning: ['M4 20.5 9.5 15l3.5 3.5L20.5 11', 'M20.5 16v-5h-5'],

  /* --- eylemler --- */
  plus: ['M12 5v14', 'M5 12h14'],
  close: ['M6.5 6.5 17.5 17.5', 'M17.5 6.5 6.5 17.5'],
  check: ['M4.5 12.5 9.5 17.5 19.5 6.5'],
  edit: ['M4.5 19.5h4L20 8a2.5 2.5 0 0 0-3.5-3.5L5 16v3.5z', 'M14.5 6.5 17.5 9.5'],
  trash: [
    'M4 7h16',
    'M9 7V5.5A1.5 1.5 0 0 1 10.5 4h3A1.5 1.5 0 0 1 15 5.5V7',
    'M6.5 7l.8 12.1a1.6 1.6 0 0 0 1.6 1.4h6.2a1.6 1.6 0 0 0 1.6-1.4L17.5 7',
  ],
  search: ['M18 11a7 7 0 1 1-14 0 7 7 0 0 1 14 0z', 'M20.5 20.5 16.4 16.4'],
  refresh: ['M19.5 12a7.5 7.5 0 1 1-2.2-5.3', 'M19.5 4.5V9H15'],
  filter: ['M3.5 5.5h17l-6.5 8V20.5l-4-2v-5l-6.5-8z'],
  menu: ['M4 7h16', 'M4 12h16', 'M4 17h16'],
  logout: ['M12 3.5v8', 'M6.8 6.8a7.5 7.5 0 1 0 10.4 0'],
  external: [
    'M14 4h6v6',
    'M20 4 11.5 12.5',
    'M18 14v4.5A1.5 1.5 0 0 1 16.5 20h-11A1.5 1.5 0 0 1 4 18.5v-11A1.5 1.5 0 0 1 5.5 6H10',
  ],
  play: ['M8 5.5 19 12 8 18.5z'],

  /* --- durum --- */
  alert: ['M12 4 21 19.5H3L12 4z', 'M12 10v4', 'M12 17.15v.1'],
  info: ['M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z', 'M12 11.5v5', 'M12 7.9V8'],
  'check-circle': ['M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z', 'M8 12.2 10.8 15 16 9.5'],
  'x-circle': [
    'M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z',
    'M9.2 9.2 14.8 14.8',
    'M14.8 9.2 9.2 14.8',
  ],
  clock: ['M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0z', 'M12 7.5V12l3 2'],

  /* --- diğer --- */
  sun: [
    'M16 12a4 4 0 1 1-8 0 4 4 0 0 1 8 0z',
    'M12 2.5v2',
    'M12 19.5v2',
    'M2.5 12h2',
    'M19.5 12h2',
    'M5.15 5.15 6.6 6.6',
    'M17.4 17.4l1.45 1.45',
    'M18.85 5.15 17.4 6.6',
    'M6.6 17.4 5.15 18.85',
  ],
  moon: ['M20.5 14.8A8.5 8.5 0 0 1 9.2 3.5 8.5 8.5 0 1 0 20.5 14.8z'],
  lock: ['M6 10.5h12V20a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1v-9.5z', 'M8.5 10.5V7a3.5 3.5 0 0 1 7 0v3.5'],
  user: ['M16 8a4 4 0 1 1-8 0 4 4 0 0 1 8 0z', 'M4.5 20.5a7.5 7.5 0 0 1 15 0'],
  calendar: [
    'M4 6.5h16V20a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V6.5z',
    'M4 10.5h16',
    'M8.5 3.5v4',
    'M15.5 3.5v4',
  ],
  box: ['M4 9h16v10.5a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 4 19.5V9z', 'M2.5 5h19v4h-19z', 'M10 13h4'],
  spark: ['M13 2.5 5 13.5h6l-1 8 8-11h-6l1-8z'],
  chevron: ['M6 9.5 12 15.5 18 9.5'],
  'arrow-right': ['M4 12h15', 'M13 6l6 6-6 6'],
};

export type IconName = keyof typeof ICONS;

@Component({
  selector: 'tz-icon',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      [attr.stroke-width]="width"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
      focusable="false"
    >
      @for (d of paths(); track d) {
        <path [attr.d]="d" />
      }
    </svg>
  `,
  styles: [
    `
      /* Olcu host uzerinde duruyor: ic svg yuzde verildiginde, hostun genisligi
         de svg'den geldigi icin dongu olusup tarayici oku 300x150'ye buyutuyordu.
         Varsayilan 1em, ikonu icinde bulundugu metinle ayni boya getiriyor. */
      :host {
        display: inline-flex;
        flex: 0 0 auto;
        width: 1em;
        height: 1em;
      }
      svg {
        display: block;
        width: 100%;
        height: 100%;
      }
    `,
  ],
})
export class IconComponent {
  private readonly key = signal<string>('');

  @Input({ required: true })
  set name(value: string) {
    this.key.set(value);
  }

  @Input() width = 1.7;

  /** Bilinmeyen ad sessizce boş çiziliyor; eksik bir ikon ekranı çökertmemeli. */
  readonly paths = computed(() => ICONS[this.key()] ?? []);
}

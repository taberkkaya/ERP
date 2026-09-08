export interface MenuItem {
  label: string;
  icon: string;
  url: string;
  /** Yalnızca tam eşleşmede etkin sayılır — kök yol her şeye ön ek olduğu için gerekli. */
  exact?: boolean;
}

export interface MenuSection {
  /** Bölüm başlığı. Boş bırakılırsa satırlar başlıksız açılır. */
  title?: string;
  items: MenuItem[];
}

export const MENU: MenuSection[] = [
  {
    items: [{ label: 'Panel', icon: 'dashboard', url: '/', exact: true }],
  },
  {
    title: 'Tanımlar',
    items: [
      { label: 'Müşteriler', icon: 'customers', url: '/musteriler' },
      { label: 'Depolar', icon: 'depots', url: '/depolar' },
      { label: 'Ürünler', icon: 'products', url: '/urunler' },
      { label: 'Reçeteler', icon: 'recipes', url: '/receteler' },
    ],
  },
  {
    title: 'İş Akışı',
    items: [
      { label: 'Siparişler', icon: 'orders', url: '/siparisler' },
      { label: 'Üretim', icon: 'production', url: '/uretim' },
    ],
  },
  {
    title: 'Faturalar',
    items: [
      { label: 'Alış Faturaları', icon: 'invoice-in', url: '/faturalar/alis' },
      { label: 'Satış Faturaları', icon: 'invoice-out', url: '/faturalar/satis' },
    ],
  },
];

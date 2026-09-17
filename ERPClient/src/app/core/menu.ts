export interface MenuItem {
  label: string;
  icon: string;
  url: string;
  /** Yalnızca tam eşleşmede etkin sayılır — kök yol her şeye ön ek olduğu için gerekli. */
  exact?: boolean;

  /**
   * Yalnızca yöneticiye gösterilir. Sunucu da bu ekranın uçlarını "Admin"
   * politikasıyla kapatıyor; menüde bırakmak kullanıcıyı çalışmayan bir sayfaya
   * götürürdü. Demo jetonu yönetici işareti taşımadığı için demoda da gizli.
   */
  adminOnly?: boolean;
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
    title: 'Yönetim',
    items: [
      { label: 'Kullanıcılar', icon: 'user', url: '/kullanicilar', adminOnly: true },
    ],
  },
];

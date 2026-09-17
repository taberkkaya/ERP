/**
 * API'nin döndürdüğü iş nesneleri.
 *
 * Formlara bağlanan modeller sınıf: şablon `new CustomerModel()` ile boş bir kayıt
 * açıyor ve her alanın tanımlı bir başlangıç değeri olmadan `ngModel` iki yönlü
 * bağlanmıyor. Yalnızca okunan yanıtlar arayüz olarak duruyor.
 */

export class CustomerModel {
  id = '';
  name = '';
  taxDepartment = '';
  taxNumber = '';
  city = '';
  town = '';
  address = '';
}

export class DepotModel {
  id = '';
  name = '';
  city = '';
  town = '';
  address = '';
}

/** Sunucudaki SmartEnum'ların istemci karşılığı: değer + görünen ad. */
export interface EnumModel {
  value: number;
  name: string;
}

export class ProductModel {
  id = '';
  name = '';
  productType?: EnumModel;
  /** Formdan gönderilen tür; sunucu enum'u bu değerden çözüyor. */
  productTypeValue = 1;
  /** Reçete ve ihtiyaç planı ekranlarında kullanılan miktar. */
  quantity = 0;
  stock = 0;
}

export class RecipeDetailModel {
  id = '';
  recipeId = '';
  productId = '';
  product?: ProductModel;
  quantity = 1;
}

export class RecipeModel {
  id = '';
  productId = '';
  product?: ProductModel;
  details?: RecipeDetailModel[];
}

export class OrderDetailModel {
  id = '';
  orderId = '';
  productId = '';
  product?: ProductModel;
  quantity = 0;
  price = 0;
}

export class OrderModel {
  id = '';
  orderNumber = 0;
  orderNumberYear = 0;
  number = '';
  date = '';
  deliveryDate = '';
  customerId = '';
  customer?: CustomerModel;
  details?: OrderDetailModel[];
  status?: EnumModel;
}

/**
 * Panodaki stok özeti. Sunucudaki StockMovements/GetSummary karşılığı.
 *
 * Faturalar Defter'de kesiliyor; burada yalnızca onların stokta bıraktığı iz
 * var. Kaynak kırılımı bu yüzden anlamlı: aynı tabloda üretimin doğurduğu
 * hareketle faturanın doğurduğu hareket yan yana duruyor.
 */
export interface StockSummaryLine {
  source: string;
  entryValue: number;
  exitValue: number;
  movementCount: number;
}

export interface StockSummaryModel {
  entryValue: number;
  exitValue: number;
  movementCount: number;
  bySource: StockSummaryLine[];
}

export class ProductionModel {
  id = '';
  productId = '';
  product?: ProductModel;
  depotId = '';
  depot?: DepotModel;
  quantity = 0;
  createdAt = '';
}

/**
 * Kip icindeki taslaklar. Okunan modelde kalem listesi bos gelebiliyor; formda
 * ise her zaman bir dizi var, bu yuzden gerekli olarak daraltiliyor.
 */
export type OrderDraft = OrderModel & { details: OrderDetailModel[] };

export interface RequirementsPlanningModel {
  date: string;
  title: string;
  products: ProductModel[];
}

/** Sipariş durumları — sunucudaki OrderStatusEnum ile aynı değerler. */
export const OrderStatus = {
  Pending: 1,
  Planned: 2,
  Completed: 3,
} as const;

export const productTypes: EnumModel[] = [
  { value: 1, name: 'Mamul' },
  { value: 2, name: 'Yarı Mamul' },
];

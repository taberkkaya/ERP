export class ProductModel {
  id: string = '';
  name: string = '';
  productType: ProductTypeModel = new ProductTypeModel();
  productTypeValue: number = 0;
  quantity: number = 0;
}

export class ProductTypeModel {
  name: string = '';
  value: number = 0;
}

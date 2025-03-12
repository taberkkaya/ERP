import { Component, ElementRef, ViewChild } from '@angular/core';
import { RecipeDetailModel } from '../../models/recipe-detail.model';
import { SharedModule } from '../../modules/shared.module';
import { RecipeDetailPipe } from '../../pipes/recipe-detail.pipe';
import { ProductModel } from '../../models/product.model';
import { RecipeModel } from '../../models/recipe.model';
import { HttpService } from '../../services/http.service';
import { SwalService } from '../../services/swal.service';
import { NgForm } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-recipe-details',
  standalone: true,
  imports: [SharedModule, RecipeDetailPipe],
  templateUrl: './recipe-details.component.html',
  styleUrl: './recipe-details.component.css',
})
export class RecipeDetailsComponent {
  recipe: RecipeModel = new RecipeModel();
  recipeId: string = '';
  products: ProductModel[] = [];

  search: string = '';

  createModel: RecipeDetailModel = new RecipeDetailModel();
  updateModel: RecipeDetailModel = new RecipeDetailModel();

  isUpdateFormActive: boolean = false;

  constructor(
    private http: HttpService,
    private swal: SwalService,
    private activated: ActivatedRoute
  ) {
    this.activated.params.subscribe((res) => {
      this.recipeId = res['id'];
      this.getRecipeById();
      this.createModel.recipeId = this.recipeId;
    });
  }

  ngOnInit(): void {
    this.getAllProducts();
  }

  getAllProducts() {
    this.http.post<ProductModel[]>('Products/GetAll', {}, (res) => {
      this.products = res.filter((p) => p.productType.value == 2);
    });
  }

  getRecipeById() {
    this.http.post<RecipeModel>(
      'RecipeDetails/GetRecipeByIdWithDetails',
      { recipeId: this.recipeId },
      (res) => {
        this.recipe = res;
      }
    );
  }

  create(form: NgForm) {
    if (form.valid)
      this.http.post<string>(
        'RecipeDetails/Create',
        this.createModel,
        (res) => {
          this.swal.callToast(res);
          this.createModel = new RecipeDetailModel();
          this.createModel.recipeId = this.recipeId;
          this.getRecipeById();
        }
      );
  }

  deleteById(model: RecipeDetailModel) {
    this.swal.callSwal(
      'Reçetedeki Ürünü Sil?',
      `${model.product.name} ürününü silmek istiyor musunuz?`,
      () => {
        this.http.post<string>(
          'RecipeDetails/DeleteById',
          { id: model.id },
          (res) => {
            this.getRecipeById();
            this.swal.callToast(res, 'info');
          }
        );
      }
    );
  }

  get(model: RecipeDetailModel) {
    this.updateModel = { ...model };
    this.updateModel.product =
      this.products.find((p) => p.id == this.updateModel.productId) ??
      new ProductModel();
    this.isUpdateFormActive = true;
  }

  update(form: NgForm) {
    if (form.valid)
      this.http.post<string>(
        'RecipeDetails/Update',
        this.updateModel,
        (res) => {
          this.swal.callToast(res, 'info');
          this.getRecipeById();
          this.isUpdateFormActive = false;
        }
      );
  }
}

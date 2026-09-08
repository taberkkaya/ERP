import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';
import { loadRuntimeConfig } from './app/core/api';

// Calisma zamani yapilandirmasi (API adresi) uygulamadan once okunuyor: ilk
// istek acilisla birlikte gidiyor, adres o ana kadar cozulmus olmali.
loadRuntimeConfig().then(() =>
  bootstrapApplication(AppComponent, appConfig).catch((err) => console.error(err))
);

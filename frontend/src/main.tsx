import { createRoot } from 'react-dom/client';
import CssBaseline from '@mui/material/CssBaseline';
import './index.css';
import App from './App.tsx';

// Not: StrictMode bilinçli olarak kullanılmıyor; geliştirme modunda effect'lerin
// iki kez çalışması html5-qrcode kamera başlat/durdur akışını bozuyor.
createRoot(document.getElementById('root')!).render(
  <>
    <CssBaseline />
    <App />
  </>
);

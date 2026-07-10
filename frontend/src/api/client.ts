import axios from 'axios';

// Netlify/prod ortamında VITE_API_URL env değişkeniyle geçersiz kılınır (bkz. netlify.toml, README.md).
// Yerel geliştirmede .env dosyası yoksa localhost:5200'e düşer.
export const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5200/api';

// Backend PascalCase döner; frontend camelCase kullanır (kural 3.7).
// İstek gövdeleri PascalCase'e, yanıtlar camelCase'e çevrilir.

const isPlainObject = (value: unknown): value is Record<string, unknown> =>
  typeof value === 'object' &&
  value !== null &&
  !Array.isArray(value) &&
  Object.getPrototypeOf(value) === Object.prototype;

const convertKeys = (
  value: unknown,
  convert: (key: string) => string
): unknown => {
  if (Array.isArray(value)) {
    return value.map((item) => convertKeys(item, convert));
  }
  if (isPlainObject(value)) {
    return Object.fromEntries(
      Object.entries(value).map(([key, val]) => [
        convert(key),
        convertKeys(val, convert),
      ])
    );
  }
  return value;
};

const toCamel = (key: string) => key.charAt(0).toLowerCase() + key.slice(1);
const toPascal = (key: string) => key.charAt(0).toUpperCase() + key.slice(1);

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
});

apiClient.interceptors.request.use((config) => {
  if (config.data instanceof FormData) {
    // multipart/form-data isteğinde varsayılan 'application/json' header'ı boundary'siz kalıp
    // isteği bozar (415 döner) — axios/tarayıcının doğru boundary'li header'ı kendi eklemesine izin ver.
    delete config.headers['Content-Type'];
  } else if (config.data) {
    config.data = convertKeys(config.data, toPascal);
  }
  const token = sessionStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => {
    if (response.data) {
      response.data = convertKeys(response.data, toCamel);
    }
    return response;
  },
  (error) => {
    if (error.response?.data) {
      error.response.data = convertKeys(error.response.data, toCamel);
    }
    if (error.response?.status === 401) {
      // Token geçersiz/süresi dolmuş: oturumu temizle, kullanıcı girişe düşsün.
      sessionStorage.removeItem('token');
      sessionStorage.removeItem('companyId');
      sessionStorage.removeItem('username');
      sessionStorage.removeItem('isLoggedIn');
      window.location.reload();
    }
    return Promise.reject(error);
  }
);

export const getErrorMessage = (error: unknown): string => {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string } | undefined;
    if (data?.message) return data.message;
    if (error.response?.status === 403) return 'Bu kayda erişim yetkiniz yok.';
    if (error.response?.status === 400) return 'Geçersiz istek.';
  }
  return 'Beklenmeyen bir hata oluştu. API çalışıyor mu?';
};

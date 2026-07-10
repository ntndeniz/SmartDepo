import { apiClient } from './client';
import type {
  ApiResponse,
  Box,
  Brand,
  DispatchOrder,
  GoodsReceipt,
  GoodsReceiptItem,
  Location,
  LoginResult,
  PagedResponse,
  ParsedStoreOrder,
  BulkCreateProductsResult,
  CompanySettings,
  Product,
  Store,
  StoreOrder,
  DispatchPallet,
  UnpalletizedBox,
  User,
  WeeklyReport,
} from '../types';

export interface ListParams {
  companyId: string;
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
  isOccupied?: boolean;
  BrandId?: number;
  fromDate?: string;
  toDate?: string;
}

const list = async <T>(path: string, params: ListParams) => {
  const { data } = await apiClient.get<PagedResponse<T>>(`${path}/list`, { params });
  return data;
};

export const authApi = {
  login: (body: { companyId: string; username: string; password: string }) =>
    apiClient.post<ApiResponse<LoginResult>>('/auth/login', body),
};

export const userApi = {
  list: (params: ListParams) => list<User>('/users', params),
  create: (body: { companyId: string; username: string; password: string; role: 'Admin' | 'Staff' }) =>
    apiClient.post<ApiResponse<User>>('/users/create', body),
  delete: (body: { id: number; companyId: string }) =>
    apiClient.post<ApiResponse<boolean>>('/users/delete', body),
};

export const brandApi = {
  list: (params: ListParams) => list<Brand>('/brands', params),
  create: (body: { companyId: string; name: string }) =>
    apiClient.post<ApiResponse<Brand>>('/brands/create', body),
  update: (body: { id: number; companyId: string; name: string }) =>
    apiClient.post<ApiResponse<Brand>>('/brands/update', body),
  delete: (body: { id: number; companyId: string }) =>
    apiClient.post<ApiResponse<boolean>>('/brands/delete', body),
};

export const productApi = {
  list: (params: ListParams) => list<Product>('/products', params),
  getByBarcode: (companyId: string, barcode: string) =>
    apiClient.get<ApiResponse<Product>>('/products/by-barcode', {
      params: { companyId, barcode },
    }),
  create: (body: { companyId: string; name: string; color: string; unit: string; BrandId: number }) =>
    apiClient.post<ApiResponse<Product>>('/products/create', body),
  update: (body: { id: number; companyId: string; name: string; color: string; unit: string; BrandId: number }) =>
    apiClient.post<ApiResponse<Product>>('/products/update', body),
  delete: (body: { id: number; companyId: string }) =>
    apiClient.post<ApiResponse<boolean>>('/products/delete', body),
  bulkCreate: (body: {
    companyId: string;
    BrandId: number;
    items: { name: string; color: string; unit: string }[];
  }) => apiClient.post<ApiResponse<BulkCreateProductsResult>>('/products/bulk-create', body),
};

export const goodsReceiptApi = {
  list: (params: ListParams) => list<GoodsReceipt>('/goods-receipts', params),
  listItems: async (params: ListParams) => {
    const { data } = await apiClient.get<PagedResponse<GoodsReceiptItem>>(
      '/goods-receipts/items',
      { params }
    );
    return data;
  },
  createSession: (body: { companyId: string }) =>
    apiClient.post<ApiResponse<GoodsReceipt>>('/goods-receipts/create', body),
  scanItem: (body: {
    companyId: string;
    GoodsReceiptId: number;
    productBarcode: string;
    quantity: number;
    desi?: number | null;
    createdBy: string;
  }) => apiClient.post<ApiResponse<GoodsReceipt>>('/goods-receipts/scan-item', body),
  delete: (body: { id: number; companyId: string }) =>
    apiClient.post<ApiResponse<boolean>>('/goods-receipts/delete', body),
};

export const boxApi = {
  list: (params: ListParams) => list<Box>('/boxes', params),
  create: (body: { companyId: string; createdBy: string; productId: number; quantity: number; desi?: number | null }) =>
    apiClient.post<ApiResponse<Box>>('/boxes/create', body),
  update: (body: { id: number; companyId: string; quantity: number; desi?: number | null; reason?: string }) =>
    apiClient.post<ApiResponse<Box>>('/boxes/update', body),
  delete: (body: { id: number; companyId: string }) =>
    apiClient.post<ApiResponse<boolean>>('/boxes/delete', body),
};

export const locationApi = {
  list: (params: ListParams) => list<Location>('/locations', params),
  generate: (body: { companyId: string }) =>
    apiClient.post<ApiResponse<{ createdCount: number; totalCount: number }>>('/locations/generate', body),
  delete: (body: { id: number; companyId: string }) =>
    apiClient.post<ApiResponse<boolean>>('/locations/delete', body),
  assignBox: (body: { companyId: string; LocationId: number; BoxBarcode: string }) =>
    apiClient.post<ApiResponse<Location>>('/locations/assign-box', body),
  release: (body: { companyId: string; LocationId: number }) =>
    apiClient.post<ApiResponse<Location>>('/locations/release', body),
};

export const storeApi = {
  list: (params: ListParams) => list<Store>('/stores', params),
  create: (body: { companyId: string; name: string; address: string }) =>
    apiClient.post<ApiResponse<Store>>('/stores/create', body),
  update: (body: { id: number; companyId: string; name: string; address: string }) =>
    apiClient.post<ApiResponse<Store>>('/stores/update', body),
  delete: (body: { id: number; companyId: string }) =>
    apiClient.post<ApiResponse<boolean>>('/stores/delete', body),
};

export const storeOrderApi = {
  list: (params: ListParams) => list<StoreOrder>('/store-orders', params),
  getByCode: (companyId: string, orderCode: string) =>
    apiClient.get<ApiResponse<StoreOrder>>('/store-orders/by-code', {
      params: { companyId, orderCode },
    }),
  create: (body: {
    companyId: string;
    storeName: string;
    address: string;
    items: { productId: number; color: string; quantity: number }[];
  }) => apiClient.post<ApiResponse<StoreOrder>>('/store-orders/create', body),
  parsePdf: (file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    // Content-Type kasıtlı olarak set edilmiyor: axios/tarayıcı FormData için boundary'li
    // multipart header'ı otomatik ekler; elle set edilirse boundary eksik kalıp istek bozulur.
    return apiClient.post<ApiResponse<ParsedStoreOrder>>('/store-orders/parse-pdf', formData);
  },
};

export const dispatchApi = {
  list: (params: ListParams) => list<DispatchOrder>('/dispatch-orders', params),
  createFromStoreOrder: (body: { companyId: string; storeOrderCode: string; createdBy: string }) =>
    apiClient.post<ApiResponse<DispatchOrder>>('/dispatch-orders/create-from-store-order', body),
  closeBox: (body: {
    companyId: string;
    dispatchOrderId: number;
    createdBy: string;
    items: { productBarcode: string; quantity: number }[];
  }) => apiClient.post<ApiResponse<DispatchOrder>>('/dispatch-orders/close-box', body),
  complete: (body: { companyId: string; id: number; forcePartial?: boolean }) =>
    apiClient.post<ApiResponse<DispatchOrder>>('/dispatch-orders/complete', body),
  createPallet: (body: { companyId: string; createdBy: string; boxBarcodes: string[] }) =>
    apiClient.post<ApiResponse<DispatchPallet>>('/dispatch-orders/create-pallet', body),
  getPalletByBarcode: (companyId: string, barcode: string) =>
    apiClient.get<ApiResponse<DispatchPallet>>('/dispatch-orders/pallet-by-barcode', {
      params: { companyId, barcode },
    }),
  listPallets: (params: ListParams) => list<DispatchPallet>('/dispatch-orders/pallets', params),
  listUnpalletizedBoxes: (companyId: string) =>
    apiClient.get<ApiResponse<UnpalletizedBox[]>>('/dispatch-orders/unpalletized-boxes', {
      params: { companyId },
    }),
  addBoxToPallet: (body: { companyId: string; palletId: number; boxBarcode: string }) =>
    apiClient.post<ApiResponse<DispatchPallet>>('/dispatch-orders/pallets/add-box', body),
  removeBoxFromPallet: (body: { companyId: string; palletId: number; boxBarcode: string }) =>
    apiClient.post<ApiResponse<DispatchPallet>>('/dispatch-orders/pallets/remove-box', body),
  markPalletReady: (body: { companyId: string; palletId: number }) =>
    apiClient.post<ApiResponse<DispatchPallet>>('/dispatch-orders/pallets/mark-ready', body),
  markPalletShipped: (body: { companyId: string; palletId: number }) =>
    apiClient.post<ApiResponse<DispatchPallet>>('/dispatch-orders/pallets/mark-shipped', body),
};

export const companySettingsApi = {
  get: () => apiClient.get<ApiResponse<CompanySettings>>('/company-settings'),
  update: (body: {
    companyId: string;
    CorridorCount: number;
    ZonesPerCorridor: number;
    ShelvesPerZone: number;
  }) => apiClient.post<ApiResponse<CompanySettings>>('/company-settings/update', body),
};

export const reportApi = {
  list: (params: ListParams & { reportType?: string }) => list<WeeklyReport>('/reports', params),
  generateNow: (body: { companyId: string }) =>
    apiClient.post<ApiResponse<boolean>>('/reports/generate-now', body),
  download: (id: number) =>
    apiClient.get(`/reports/${id}/download`, { responseType: 'blob' }),
};

export interface PagedResponse<T> {
  success: boolean;
  data: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
}

export type UserRole = 'Admin' | 'Staff';

export interface User {
  id: number;
  companyId: string;
  username: string;
  role: UserRole;
  createdAt: string;
}

export interface LoginResult {
  token: string;
  user: User;
}

export interface Brand {
  id: number;
  companyId: string;
  name: string;
  shortCode: string;
  createdAt: string;
}

export interface Product {
  id: number;
  companyId: string;
  name: string;
  barcode: string;
  unit: string;
  color: string;
  brandId: number;
  brandName: string;
  createdAt: string;
}

export interface GoodsReceiptItem {
  id: number;
  productId: number;
  productName: string;
  productBarcode: string;
  productColor: string;
  brandId: number;
  brandName: string;
  boxId: number;
  boxBarcode: string;
  desi?: number | null;
  countedQuantity: number;
  cumulativeQuantity: number;
  createdAt: string;
}

export interface GoodsReceipt {
  id: number;
  companyId: string;
  receivedAt: string;
  items: GoodsReceiptItem[];
}

export interface Box {
  id: number;
  companyId: string;
  barcode: string;
  productId: number;
  productName: string;
  productBarcode: string;
  productColor: string;
  quantity: number;
  desi?: number | null;
  status: string;
  createdBy: string;
  createdAt: string;
}

export interface Location {
  id: number;
  companyId: string;
  corridorNo: number;
  zoneNo: number;
  shelfNo: number;
  barcode: string;
  isOccupied: boolean;
  currentBoxId?: number | null;
  currentBoxBarcode?: string | null;
  currentBoxProductName?: string | null;
}

export interface StoreOrderItem {
  id: number;
  productId: number;
  productBarcode: string;
  productName: string;
  color: string;
  quantity: number;
}

export interface StoreOrder {
  id: number;
  companyId: string;
  orderCode: string;
  storeId: string;
  storeName: string;
  address: string;
  createdAt: string;
  items: StoreOrderItem[];
  dispatchStatus: string | null;
  dispatchOrderId: number | null;
}

export interface PickSuggestion {
  boxBarcode: string;
  availableQuantity: number;
  status: string;
  locationBarcode: string | null;
}

export interface DispatchOrderItem {
  id: number;
  productId: number;
  productBarcode: string;
  productName: string;
  color: string;
  requestedQuantity: number;
  pickedQuantity: number;
  suggestions: PickSuggestion[];
}

export interface DispatchBoxItem {
  id: number;
  sourceBoxId: number;
  sourceBoxBarcode: string;
  productId: number;
  productName: string;
  quantity: number;
  pickedFromLocationBarcode?: string | null;
}

export interface DispatchBox {
  id: number;
  dispatchOrderId: number;
  barcode: string;
  createdBy: string;
  createdAt: string;
  items: DispatchBoxItem[];
}

export interface DispatchOrder {
  id: number;
  companyId: string;
  storeOrderId: number;
  storeOrderCode: string;
  storeId: string;
  storeName: string;
  address: string;
  status: string;
  createdBy: string;
  createdAt: string;
  items: DispatchOrderItem[];
  boxes: DispatchBox[];
}

export type DispatchPalletStatus = 'Preparing' | 'Ready' | 'Shipped';

export interface DispatchPallet {
  id: number;
  companyId: string;
  barcode: string;
  status: DispatchPalletStatus;
  storeId: string;
  storeName: string;
  createdBy: string;
  createdAt: string;
  boxCount: number;
  totalItemQuantity: number;
  boxBarcodes: string[];
}

export interface UnpalletizedBox {
  id: number;
  barcode: string;
  dispatchOrderId: number;
  storeId: string;
  storeName: string;
  createdAt: string;
  itemQuantity: number;
  itemsSummary: string;
}

export interface ParsedOrderItem {
  productId: number | null;
  productBarcode: string;
  productName: string;
  color: string;
  quantity: number;
  matched: boolean;
}

export interface ParsedStoreOrder {
  storeId: string | null;
  storeName: string | null;
  address: string | null;
  items: ParsedOrderItem[];
  warnings: string[];
}

export interface BulkCreateRowResult {
  rowNumber: number;
  name: string | null;
  success: boolean;
  error: string | null;
  barcode: string | null;
}

export interface BulkCreateProductsResult {
  createdCount: number;
  rows: BulkCreateRowResult[];
}

export interface CompanySettings {
  corridorCount: number;
  zonesPerCorridor: number;
  shelvesPerZone: number;
  isConfigured: boolean;
  updatedAt: string | null;
}

export interface Store {
  id: number;
  companyId: string;
  storeCode: string;
  name: string;
  address: string;
  createdAt: string;
}

export interface WeeklyReport {
  id: number;
  reportType: 'GoodsReceipts' | 'Dispatches';
  weekStart: string;
  weekEnd: string;
  fileName: string;
  rowCount: number;
  generatedAt: string;
}

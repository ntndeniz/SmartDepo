import { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  IconButton,
  MenuItem,
  Snackbar,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import type { Brand, Product } from '../types';
import { getErrorMessage } from '../api/client';
import { brandApi, productApi } from '../api/services';
import PagedTable, { type Column } from './PagedTable';
import BrandFormModal from './BrandFormModal';
import ProductFormModal from './ProductFormModal';
import ProductBulkUploadModal from './ProductBulkUploadModal';
import DeleteConfirmModal from './DeleteConfirmModal';
import SummaryCards from './SummaryCards';
import PrintBarcodeButton from './PrintBarcodeButton';
import { formatDate } from '../utils/formatDate';

interface Props {
  companyId: string;
  isAdmin: boolean;
}

export default function BrandTable({ companyId, isAdmin }: Props) {
  const [brands, setBrands] = useState<Brand[]>([]);
  const [brandTotalCount, setBrandTotalCount] = useState(0);
  const [brandPage, setBrandPage] = useState(1);
  const [brandPageSize, setBrandPageSize] = useState(25);
  const [brandSearch, setBrandSearch] = useState('');
  const [selectedBrandId, setSelectedBrandId] = useState<number | null>(null);
  const [selectedBrand, setSelectedBrand] = useState<Brand | null>(null);

  const [products, setProducts] = useState<Product[]>([]);
  const [productPage, setProductPage] = useState(1);
  const [productPageSize, setProductPageSize] = useState(25);
  const [productTotalCount, setProductTotalCount] = useState(0);
  const [productSearch, setProductSearch] = useState('');

  const [brandFormOpen, setBrandFormOpen] = useState(false);
  const [editingBrand, setEditingBrand] = useState<Brand | null>(null);

  const [productFormOpen, setProductFormOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [bulkUploadOpen, setBulkUploadOpen] = useState(false);

  const [deleting, setDeleting] = useState<Brand | null>(null);
  const [deletingProduct, setDeletingProduct] = useState<Product | null>(null);

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null);

  // Markalar listesi yükle (server-side arama + sayfalama)
  const loadBrands = useCallback(async () => {
    setLoading(true);
    try {
      const res = await brandApi.list({
        companyId,
        page: brandPage,
        pageSize: brandPageSize,
        search: brandSearch,
      });
      setBrands(res.data);
      setBrandTotalCount(res.totalCount);
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setLoading(false);
    }
  }, [companyId, brandPage, brandPageSize, brandSearch]);

  // Marka dropdown'u için tüm markaları da ayrıca çekiyoruz (arama sonucu daralmış listeden bağımsız)
  const [allBrands, setAllBrands] = useState<Brand[]>([]);
  const loadAllBrands = useCallback(async () => {
    try {
      const res = await brandApi.list({ companyId, page: 1, pageSize: 1000 });
      setAllBrands(res.data);
    } catch {
      setAllBrands([]);
    }
  }, [companyId]);

  // Seçili markanın ürünlerini yükle (backend'de BrandId ile filtrelenir)
  const loadProducts = useCallback(async () => {
    if (!selectedBrandId) {
      setProducts([]);
      return;
    }

    setLoading(true);
    try {
      const res = await productApi.list({
        companyId,
        page: productPage,
        pageSize: productPageSize,
        search: productSearch,
        BrandId: selectedBrandId,
      });
      setProducts(res.data);
      setProductTotalCount(res.totalCount);
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setLoading(false);
    }
  }, [selectedBrandId, companyId, productPage, productPageSize, productSearch]);

  useEffect(() => {
    const timer = setTimeout(loadBrands, brandSearch ? 400 : 0);
    return () => clearTimeout(timer);
  }, [loadBrands, brandSearch]);

  useEffect(() => {
    loadAllBrands();
  }, [loadAllBrands]);

  useEffect(() => {
    loadProducts();
  }, [loadProducts]);

  useEffect(() => {
    if (selectedBrandId) {
      const brand = allBrands.find((b) => b.id === selectedBrandId);
      setSelectedBrand(brand ?? null);
    }
  }, [selectedBrandId, allBrands]);

  // Marka CRUD
  const handleSaveBrand = async (values: { name: string }) => {
    setSaving(true);
    try {
      if (editingBrand) {
        await brandApi.update({ id: editingBrand.id, companyId, ...values });
        setMessage({ text: 'Marka güncellendi.', error: false });
      } else {
        await brandApi.create({ companyId, ...values });
        setMessage({ text: 'Marka oluşturuldu.', error: false });
      }
      setBrandFormOpen(false);
      setEditingBrand(null);
      await Promise.all([loadBrands(), loadAllBrands()]);
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteBrand = async () => {
    if (!deleting) return;
    setSaving(true);
    try {
      await brandApi.delete({ id: deleting.id, companyId });
      setMessage({ text: 'Marka silindi.', error: false });
      setDeleting(null);
      if (selectedBrandId === deleting.id) {
        setSelectedBrandId(null);
      }
      await Promise.all([loadBrands(), loadAllBrands()]);
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  // Ürün CRUD
  const handleSaveProduct = async (values: { name: string; color: string; unit: string; BrandId: number }) => {
    setSaving(true);
    try {
      if (editingProduct) {
        await productApi.update({ id: editingProduct.id, companyId, ...values });
        setMessage({ text: 'Ürün güncellendi.', error: false });
      } else {
        // Otomatik barkod — frontend'den gönderilmez
        await productApi.create({ companyId, ...values });
        setMessage({ text: 'Ürün oluşturuldu. (Barkod otomatik üretildi)', error: false });
      }
      setProductFormOpen(false);
      setEditingProduct(null);
      await loadProducts();
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const handleDeleteProduct = async () => {
    if (!deletingProduct) return;
    setSaving(true);
    try {
      await productApi.delete({ id: deletingProduct.id, companyId });
      setMessage({ text: 'Ürün silindi.', error: false });
      setDeletingProduct(null);
      await loadProducts();
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const brandColumns: Column<Brand>[] = [
    { key: 'shortCode', label: 'Marka ID' },
    { key: 'name', label: 'Marka Adı' },
    {
      key: 'createdAt',
      label: 'Oluşturulma',
      render: (row) => formatDate(row.createdAt),
    },
  ];

  const productColumns: Column<Product>[] = [
    { key: 'barcode', label: 'Barkod' },
    { key: 'name', label: 'Ürün Adı' },
    { key: 'color', label: 'Renk' },
    { key: 'unit', label: 'Birim' },
  ];

  return (
    <>
      {/* Markalar Özeti */}
      <SummaryCards
        cards={[
          { label: 'Toplam Marka', value: brandTotalCount },
          { label: 'Seçili Marka Ürünleri', value: productTotalCount },
        ]}
      />

      {/* Markalar Tablosu */}
      <Box sx={{ mb: 4 }}>
        <Typography variant="h6" sx={{ mb: 2 }}>
          Markalar Listesi
        </Typography>
        <PagedTable
          columns={brandColumns}
          rows={brands}
          rowKey={(row) => row.id}
          totalCount={brandTotalCount}
          page={brandPage}
          pageSize={brandPageSize}
          loading={loading}
          search={brandSearch}
          onSearchChange={(v) => {
            setBrandSearch(v);
            setBrandPage(1);
          }}
          onPageChange={setBrandPage}
          onPageSizeChange={(size) => {
            setBrandPageSize(size);
            setBrandPage(1);
          }}
          onAdd={
            isAdmin
              ? () => {
                  setEditingBrand(null);
                  setBrandFormOpen(true);
                }
              : undefined
          }
          addLabel="Yeni Marka"
          renderActions={(row) => (
            <>
              {isAdmin && (
                <IconButton
                  size="small"
                  onClick={() => {
                    setEditingBrand(row);
                    setBrandFormOpen(true);
                  }}
                >
                  <EditIcon fontSize="small" />
                </IconButton>
              )}
              {isAdmin && (
                <IconButton size="small" color="error" onClick={() => setDeleting(row)}>
                  <DeleteIcon fontSize="small" />
                </IconButton>
              )}
            </>
          )}
        />
      </Box>

      {/* Marka Seçimi Dropdown */}
      <Box sx={{ mb: 4 }}>
        <TextField
          select
          fullWidth
          label="Marka Seç (Ürünleri Görmek İçin)"
          value={selectedBrandId ?? ''}
          onChange={(e) => {
            setSelectedBrandId(e.target.value ? parseInt(e.target.value as string, 10) : null);
            setProductPage(1);
          }}
        >
          {allBrands.map((b) => (
            <MenuItem key={b.id} value={b.id}>
              {b.name}
            </MenuItem>
          ))}
        </TextField>
      </Box>

      {/* Ürünler Paneli (Marka Seçilmişse) */}
      {selectedBrandId && selectedBrand && (
        <Box sx={{ bgcolor: '#f9f9f9', p: 3, borderRadius: 2 }}>
          <Typography variant="h6" sx={{ mb: 2 }}>
            {selectedBrand.name} — Ürünler
          </Typography>

          <Stack direction="row" spacing={2} sx={{ mb: 2 }}>
            <TextField
              label="Barkod veya Ürün Adı Ara"
              size="small"
              value={productSearch}
              onChange={(e) => {
                setProductSearch(e.target.value);
                setProductPage(1);
              }}
              sx={{ flex: 1 }}
            />
            {isAdmin && (
              <Button
                variant="contained"
                startIcon={<AddIcon />}
                onClick={() => {
                  setEditingProduct(null);
                  setProductFormOpen(true);
                }}
              >
                Yeni Ürün
              </Button>
            )}
            {isAdmin && (
              <Button variant="outlined" onClick={() => setBulkUploadOpen(true)}>
                Toplu Yükle (CSV)
              </Button>
            )}
          </Stack>

          {products.length === 0 ? (
            <Typography color="textSecondary">Bu markaya ait ürün yok.</Typography>
          ) : (
            <PagedTable
              columns={productColumns}
              rows={products}
              rowKey={(row) => row.id}
              totalCount={productTotalCount}
              page={productPage}
              pageSize={productPageSize}
              loading={loading}
              search={productSearch}
              onSearchChange={(v) => {
                setProductSearch(v);
                setProductPage(1);
              }}
              onPageChange={setProductPage}
              onPageSizeChange={setProductPageSize}
              renderActions={(row) => (
                <>
                  <PrintBarcodeButton value={row.barcode} label={`${row.name} (${row.color})`} />
                  {isAdmin && (
                    <IconButton
                      size="small"
                      onClick={() => {
                        setEditingProduct(row);
                        setProductFormOpen(true);
                      }}
                    >
                      <EditIcon fontSize="small" />
                    </IconButton>
                  )}
                  {isAdmin && (
                    <IconButton
                      size="small"
                      color="error"
                      onClick={() => setDeletingProduct(row)}
                    >
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  )}
                </>
              )}
            />
          )}
        </Box>
      )}

      {/* Modallar */}
      <BrandFormModal
        open={brandFormOpen}
        brand={editingBrand}
        loading={saving}
        onSave={handleSaveBrand}
        onClose={() => {
          setBrandFormOpen(false);
          setEditingBrand(null);
        }}
      />

      <ProductFormModal
        open={productFormOpen}
        companyId={companyId}
        product={editingProduct}
        initialBrandId={selectedBrandId}
        loading={saving}
        onSave={handleSaveProduct}
        onClose={() => {
          setProductFormOpen(false);
          setEditingProduct(null);
        }}
      />

      <ProductBulkUploadModal
        open={bulkUploadOpen}
        companyId={companyId}
        brandId={selectedBrandId}
        brandName={selectedBrand?.name ?? ''}
        onUploaded={loadProducts}
        onClose={() => setBulkUploadOpen(false)}
      />

      <DeleteConfirmModal
        open={deleting !== null}
        loading={saving}
        onConfirm={handleDeleteBrand}
        onClose={() => setDeleting(null)}
        title="Marka Sil"
        message={`Marka "${deleting?.name}" silinecektir. Emin misiniz?`}
      />

      <DeleteConfirmModal
        open={deletingProduct !== null}
        loading={saving}
        onConfirm={handleDeleteProduct}
        onClose={() => setDeletingProduct(null)}
        title="Ürün Sil"
        message={`Ürün "${deletingProduct?.name}" silinecektir. Emin misiniz?`}
      />

      <Snackbar
        open={message !== null}
        autoHideDuration={4000}
        onClose={() => setMessage(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity={message?.error ? 'error' : 'success'} onClose={() => setMessage(null)}>
          {message?.text}
        </Alert>
      </Snackbar>
    </>
  );
}

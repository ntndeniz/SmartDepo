import { useEffect, useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  MenuItem,
  Autocomplete,
  Box,
} from '@mui/material';
import type { Product, Brand } from '../types';
import { brandApi } from '../api/services';
import PrintBarcodeButton from './PrintBarcodeButton';

const UNITS = ['Adet', 'Kg', 'Koli', 'Paket'];

interface Props {
  open: boolean;
  companyId: string;
  product: Product | null;
  initialBrandId?: number | null;
  loading?: boolean;
  onSave: (values: { name: string; color: string; unit: string; BrandId: number }) => void;
  onClose: () => void;
}

export default function ProductFormModal({
  open,
  companyId,
  product,
  initialBrandId,
  loading,
  onSave,
  onClose,
}: Props) {
  const [name, setName] = useState('');
  const [color, setColor] = useState('');
  const [unit, setUnit] = useState('Adet');
  const [brandId, setBrandId] = useState<number | null>(null);
  const [brands, setBrands] = useState<Brand[]>([]);

  useEffect(() => {
    if (!open) return;

    // Markalar listesini yükle
    brandApi
      .list({ companyId, page: 1, pageSize: 100 })
      .then((res) => setBrands(res.data))
      .catch(() => setBrands([]));

    if (product) {
      setName(product.name);
      setColor(product.color);
      setUnit(product.unit);
      setBrandId(product.brandId);
    } else {
      setName('');
      setColor('');
      setUnit('Adet');
      setBrandId(initialBrandId ?? null);
    }
  }, [open, product, initialBrandId, companyId]);

  return (
    <>
      <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
        <DialogTitle>{product ? 'Ürün Düzenle' : 'Yeni Ürün'}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField
              label="Ürün Adı"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
              fullWidth
              autoFocus
            />
            <TextField
              label="Renk"
              value={color}
              onChange={(e) => setColor(e.target.value)}
              required
              fullWidth
            />
            <Autocomplete
              options={brands}
              getOptionLabel={(b) => b.name}
              value={brands.find((b) => b.id === brandId) ?? null}
              onChange={(_, value) => setBrandId(value?.id ?? null)}
              renderInput={(params) => (
                <TextField {...params} label="Marka" required />
              )}
            />
            <TextField
              select
              label="Birim"
              value={unit}
              onChange={(e) => setUnit(e.target.value)}
              fullWidth
            >
              {UNITS.map((u) => (
                <MenuItem key={u} value={u}>
                  {u}
                </MenuItem>
              ))}
            </TextField>
            {product && (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <TextField
                  label="Barkod (Otomatik Üretildi)"
                  value={product.barcode}
                  disabled
                  fullWidth
                  size="small"
                />
                <PrintBarcodeButton value={product.barcode} label={`${product.name} (${product.color})`} />
              </Box>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose} disabled={loading}>
            Vazgeç
          </Button>
          <Button
            variant="contained"
            disabled={loading || !name.trim() || !color.trim() || !brandId}
            onClick={() =>
              onSave({ name: name.trim(), color: color.trim(), unit, BrandId: brandId! })
            }
          >
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

import { useEffect, useState } from 'react';
import {
  Autocomplete,
  Box as MuiBox,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
} from '@mui/material';
import type { Box, Product } from '../types';
import { productApi } from '../api/services';
import PrintBarcodeButton from './PrintBarcodeButton';

interface Props {
  open: boolean;
  companyId: string;
  box: Box | null;
  loading?: boolean;
  onSave: (values: { productId: number; quantity: number; desi: number | null; reason?: string }) => void;
  onClose: () => void;
}

export default function BoxFormModal({ open, companyId, box, loading, onSave, onClose }: Props) {
  const [product, setProduct] = useState<Product | null>(null);
  const [quantity, setQuantity] = useState(1);
  const [desi, setDesi] = useState<number | null>(null);
  const [reason, setReason] = useState('');
  const [products, setProducts] = useState<Product[]>([]);
  const quantityChanged = box !== null && quantity !== box.quantity;

  useEffect(() => {
    if (!open) return;

    productApi
      .list({ companyId, page: 1, pageSize: 100 })
      .then((res) => {
        setProducts(res.data);
        if (box) {
          setProduct(res.data.find((p) => p.id === box.productId) ?? null);
        }
      })
      .catch(() => setProducts([]));

    setQuantity(box?.quantity ?? 1);
    setDesi(box?.desi ?? null);
    setReason('');
    if (!box) setProduct(null);
  }, [open, box, companyId]);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{box ? 'Koli Düzenle' : 'Yeni Koli'}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {box && (
            <MuiBox sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <TextField
                label="Koli Barkodu (Otomatik Üretildi)"
                value={box.barcode}
                disabled
                fullWidth
                size="small"
              />
              <PrintBarcodeButton value={box.barcode} label={box.productName} />
            </MuiBox>
          )}
          <Autocomplete
            options={products}
            value={product}
            disabled={box !== null} // koli tek ürün taşır; üründen sonra değiştirilemez
            isOptionEqualToValue={(a, b) => a.id === b.id}
            getOptionLabel={(p) => `${p.name} (${p.barcode})`}
            onChange={(_, value) => setProduct(value)}
            renderInput={(params) => <TextField {...params} label="Ürün (tek çeşit)" required />}
          />
          <TextField
            label="Miktar"
            type="number"
            value={quantity}
            onChange={(e) => setQuantity(parseInt(e.target.value, 10) || 0)}
            required
            fullWidth
            helperText={box && quantity === 0 ? 'Miktar 0 olursa koli otomatik olarak kaldırılır.' : undefined}
          />
          <TextField
            label="Desi (opsiyonel)"
            type="number"
            value={desi ?? ''}
            onChange={(e) => setDesi(e.target.value ? parseFloat(e.target.value) : null)}
            slotProps={{ htmlInput: { step: '0.01' } }}
            fullWidth
          />
          {quantityChanged && (
            <TextField
              label="Miktar Değişikliği Gerekçesi (zorunlu)"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Sayım farkı, fire, hasar vb."
              required
              fullWidth
              multiline
              minRows={2}
            />
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Vazgeç
        </Button>
        <Button
          variant="contained"
          disabled={
            loading ||
            product === null ||
            quantity < 0 ||
            (!box && quantity <= 0) ||
            (quantityChanged && reason.trim().length === 0)
          }
          onClick={() => onSave({ productId: product!.id, quantity, desi, reason: quantityChanged ? reason : undefined })}
        >
          Kaydet
        </Button>
      </DialogActions>
    </Dialog>
  );
}

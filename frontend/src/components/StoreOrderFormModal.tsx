import { useEffect, useState } from 'react';
import {
  Alert,
  Autocomplete,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import type { Product, Store } from '../types';
import { productApi, storeApi } from '../api/services';

interface DraftItem {
  product: Product;
  color: string;
  quantity: number;
}

interface Props {
  open: boolean;
  companyId: string;
  loading?: boolean;
  onSave: (values: {
    storeName: string;
    address: string;
    items: { productId: number; color: string; quantity: number }[];
  }) => void;
  onClose: () => void;
}

export default function StoreOrderFormModal({ open, companyId, loading, onSave, onClose }: Props) {
  const [storeName, setStoreName] = useState('');
  const [address, setAddress] = useState('');
  const [products, setProducts] = useState<Product[]>([]);
  const [stores, setStores] = useState<Store[]>([]);
  const [items, setItems] = useState<DraftItem[]>([]);

  const [draftProduct, setDraftProduct] = useState<Product | null>(null);
  const [draftColor, setDraftColor] = useState('');
  const [draftQuantity, setDraftQuantity] = useState(1);

  useEffect(() => {
    if (!open) {
      setStoreName('');
      setAddress('');
      setItems([]);
      setDraftProduct(null);
      setDraftColor('');
      setDraftQuantity(1);
      return;
    }
    productApi
      .list({ companyId, page: 1, pageSize: 1000 })
      .then((res) => setProducts(res.data))
      .catch(() => setProducts([]));
    storeApi
      .list({ companyId, page: 1, pageSize: 1000 })
      .then((res) => setStores(res.data))
      .catch(() => setStores([]));
  }, [open, companyId]);

  const handleAddItem = () => {
    if (!draftProduct || draftQuantity <= 0) return;
    setItems((prev) => [
      ...prev,
      { product: draftProduct, color: draftColor.trim() || draftProduct.color, quantity: draftQuantity },
    ]);
    setDraftProduct(null);
    setDraftColor('');
    setDraftQuantity(1);
  };

  const handleRemoveItem = (index: number) => {
    setItems((prev) => prev.filter((_, i) => i !== index));
  };

  const handleSubmit = () => {
    onSave({
      storeName: storeName.trim(),
      address: address.trim(),
      items: items.map((i) => ({ productId: i.product.id, color: i.color, quantity: i.quantity })),
    });
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Yeni Mağaza Siparişi</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <Autocomplete
            freeSolo
            options={stores}
            value={storeName}
            inputValue={storeName}
            getOptionLabel={(option) => (typeof option === 'string' ? option : `${option.name} (${option.storeCode})`)}
            onInputChange={(_, value) => setStoreName(value)}
            onChange={(_, value) => {
              if (value && typeof value !== 'string') {
                setStoreName(value.name);
                setAddress(value.address);
              }
            }}
            renderInput={(params) => (
              <TextField {...params} label="Mağaza Adı" required helperText="Kayıtlı bir mağaza seçin ya da yeni bir isim yazın." />
            )}
          />
          <TextField
            label="Adres Bilgisi"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            required
            fullWidth
            multiline
            minRows={2}
          />

          <Typography variant="subtitle2">İstenen Ürünler</Typography>
          <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
            <Autocomplete
              sx={{ flex: 1 }}
              size="small"
              options={products}
              value={draftProduct}
              isOptionEqualToValue={(a, b) => a.id === b.id}
              getOptionLabel={(p) => `${p.name} (${p.barcode}) — ${p.color}`}
              onChange={(_, value) => {
                setDraftProduct(value);
                setDraftColor(value?.color ?? '');
              }}
              renderInput={(params) => <TextField {...params} label="Ürün" />}
            />
            <TextField
              size="small"
              label="Renk"
              value={draftColor}
              onChange={(e) => setDraftColor(e.target.value)}
              sx={{ width: 130 }}
            />
            <TextField
              size="small"
              type="number"
              label="Miktar"
              value={draftQuantity}
              onChange={(e) => setDraftQuantity(parseInt(e.target.value, 10) || 0)}
              slotProps={{ htmlInput: { min: 1 } }}
              sx={{ width: 110 }}
            />
            <Button
              variant="outlined"
              startIcon={<AddIcon />}
              onClick={handleAddItem}
              disabled={!draftProduct || draftQuantity <= 0}
            >
              Ekle
            </Button>
          </Stack>

          {items.length === 0 ? (
            <Alert severity="info">Henüz ürün eklenmedi.</Alert>
          ) : (
            items.map((item, index) => (
              <Stack
                key={index}
                direction="row"
                spacing={2}
                sx={{ alignItems: 'center', border: '1px solid', borderColor: 'divider', borderRadius: 1, p: 1 }}
              >
                <Typography variant="body2" sx={{ flex: 1 }}>
                  {item.product.name} ({item.product.barcode}) — Renk: {item.color} — Miktar: {item.quantity}
                </Typography>
                <IconButton size="small" color="error" onClick={() => handleRemoveItem(index)}>
                  <DeleteIcon fontSize="small" />
                </IconButton>
              </Stack>
            ))
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Vazgeç
        </Button>
        <Button
          variant="contained"
          disabled={loading || !storeName.trim() || !address.trim() || items.length === 0}
          onClick={handleSubmit}
        >
          Sipariş Oluştur
        </Button>
      </DialogActions>
    </Dialog>
  );
}

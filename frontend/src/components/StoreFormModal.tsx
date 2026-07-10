import { useEffect, useState } from 'react';
import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
} from '@mui/material';
import type { Store } from '../types';

interface Props {
  open: boolean;
  store: Store | null;
  loading?: boolean;
  onSave: (values: { name: string; address: string }) => void;
  onClose: () => void;
}

export default function StoreFormModal({ open, store, loading, onSave, onClose }: Props) {
  const [name, setName] = useState('');
  const [address, setAddress] = useState('');

  useEffect(() => {
    if (open) {
      setName(store?.name ?? '');
      setAddress(store?.address ?? '');
    }
  }, [open, store]);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{store ? 'Mağaza Düzenle' : 'Yeni Mağaza'}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {store && (
            <TextField label="Mağaza Kodu (Otomatik)" value={store.storeCode} disabled fullWidth size="small" />
          )}
          <TextField
            autoFocus
            label="Mağaza Adı"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            fullWidth
          />
          <TextField
            label="Adres"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            required
            fullWidth
            multiline
            minRows={2}
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Vazgeç
        </Button>
        <Button
          variant="contained"
          disabled={loading || !name.trim() || !address.trim()}
          onClick={() => onSave({ name: name.trim(), address: address.trim() })}
        >
          Kaydet
        </Button>
      </DialogActions>
    </Dialog>
  );
}

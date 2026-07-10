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
import type { Brand } from '../types';

interface Props {
  open: boolean;
  brand: Brand | null;
  loading?: boolean;
  onSave: (values: { name: string }) => void;
  onClose: () => void;
}

export default function BrandFormModal({ open, brand, loading, onSave, onClose }: Props) {
  const [name, setName] = useState('');

  useEffect(() => {
    if (open) {
      setName(brand?.name ?? '');
    }
  }, [open, brand]);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{brand ? 'Marka Düzenle' : 'Yeni Marka'}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          <TextField
            autoFocus
            label="Marka Adı"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
            fullWidth
          />
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={loading}>
          Vazgeç
        </Button>
        <Button
          variant="contained"
          disabled={loading || !name.trim()}
          onClick={() => onSave({ name: name.trim() })}
        >
          Kaydet
        </Button>
      </DialogActions>
    </Dialog>
  );
}

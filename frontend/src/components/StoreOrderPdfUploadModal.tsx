import { useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import UploadFileIcon from '@mui/icons-material/UploadFile';
import DeleteIcon from '@mui/icons-material/Delete';
import type { ParsedOrderItem, ParsedStoreOrder } from '../types';
import { getErrorMessage } from '../api/client';
import { storeOrderApi } from '../api/services';

interface Props {
  open: boolean;
  loading?: boolean;
  onConfirm: (values: {
    storeName: string;
    address: string;
    items: { productId: number; color: string; quantity: number }[];
  }) => void;
  onClose: () => void;
}

export default function StoreOrderPdfUploadModal({ open, loading, onConfirm, onClose }: Props) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [parsing, setParsing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [parsed, setParsed] = useState<ParsedStoreOrder | null>(null);
  const [pdfStoreCode, setPdfStoreCode] = useState('');
  const [storeName, setStoreName] = useState('');
  const [address, setAddress] = useState('');
  const [items, setItems] = useState<ParsedOrderItem[]>([]);

  const reset = () => {
    setParsed(null);
    setError(null);
    setPdfStoreCode('');
    setStoreName('');
    setAddress('');
    setItems([]);
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const handleClose = () => {
    reset();
    onClose();
  };

  const handleFileSelect = async (file: File) => {
    setParsing(true);
    setError(null);
    try {
      const res = await storeOrderApi.parsePdf(file);
      if (res.data.success && res.data.data) {
        const data = res.data.data;
        setParsed(data);
        setPdfStoreCode(data.storeId ?? '');
        setStoreName(data.storeName ?? '');
        setAddress(data.address ?? '');
        setItems(data.items.filter((i) => i.matched));
      } else {
        setError(res.data.message || 'PDF ayrıştırılamadı.');
      }
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setParsing(false);
    }
  };

  const handleQuantityChange = (index: number, value: number) => {
    setItems((prev) => prev.map((it, i) => (i === index ? { ...it, quantity: value } : it)));
  };

  const handleRemoveRow = (index: number) => {
    setItems((prev) => prev.filter((_, i) => i !== index));
  };

  const canConfirm =
    storeName.trim() && address.trim() && items.length > 0 && items.every((i) => i.quantity > 0);

  const handleConfirm = () => {
    onConfirm({
      storeName: storeName.trim(),
      address: address.trim(),
      items: items.map((i) => ({ productId: i.productId!, color: i.color, quantity: i.quantity })),
    });
  };

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
      <DialogTitle>PDF'ten Mağaza Siparişi Yükle</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {!parsed && (
            <Box sx={{ textAlign: 'center', py: 4 }}>
              <input
                ref={fileInputRef}
                type="file"
                accept="application/pdf"
                hidden
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) handleFileSelect(file);
                }}
              />
              <Button
                variant="contained"
                startIcon={<UploadFileIcon />}
                disabled={parsing}
                onClick={() => fileInputRef.current?.click()}
              >
                {parsing ? 'Ayrıştırılıyor...' : 'PDF Seç'}
              </Button>
              <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
                Yalnızca sabit "Mağaza Sipariş Formu" şablonundaki PDF'ler desteklenir.{' '}
                <a href="/magaza-siparis-sablonu.pdf" target="_blank" rel="noreferrer">
                  Örnek şablonu indir
                </a>
                .
              </Typography>
              {error && (
                <Alert severity="error" sx={{ mt: 2 }}>
                  {error}
                </Alert>
              )}
            </Box>
          )}

          {parsed && (
            <>
              {parsed.warnings.length > 0 && (
                <Alert severity="warning">
                  {parsed.warnings.map((w) => (
                    <div key={w}>{w}</div>
                  ))}
                </Alert>
              )}
              <Typography variant="body2" color="text.secondary">
                Aşağıdaki bilgileri onaylamadan önce kontrol edin/düzeltin. Eşleşmeyen barkodlar listeye
                dahil edilmedi.
              </Typography>
              <Stack direction="row" spacing={2}>
                <TextField
                  label="PDF'teki Mağaza Kodu (bilgi amaçlı)"
                  value={pdfStoreCode}
                  disabled
                  fullWidth
                  helperText="Sistemdeki gerçek mağaza kimliği isme göre otomatik eşleştirilir."
                />
                <TextField
                  label="Mağaza Adı"
                  value={storeName}
                  onChange={(e) => setStoreName(e.target.value)}
                  fullWidth
                  required
                />
              </Stack>
              <TextField
                label="Teslimat Adresi"
                value={address}
                onChange={(e) => setAddress(e.target.value)}
                fullWidth
                required
              />

              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Barkod</TableCell>
                    <TableCell>Ürün</TableCell>
                    <TableCell>Renk</TableCell>
                    <TableCell>Miktar</TableCell>
                    <TableCell />
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} align="center">
                        Eşleşen ürün satırı yok.
                      </TableCell>
                    </TableRow>
                  ) : (
                    items.map((item, index) => (
                      <TableRow key={`${item.productBarcode}-${index}`}>
                        <TableCell>{item.productBarcode}</TableCell>
                        <TableCell>{item.productName}</TableCell>
                        <TableCell>{item.color}</TableCell>
                        <TableCell>
                          <TextField
                            size="small"
                            type="number"
                            value={item.quantity}
                            onChange={(e) => handleQuantityChange(index, parseInt(e.target.value, 10) || 0)}
                            sx={{ width: 90 }}
                          />
                        </TableCell>
                        <TableCell>
                          <IconButton size="small" onClick={() => handleRemoveRow(index)}>
                            <DeleteIcon fontSize="small" />
                          </IconButton>
                        </TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={loading}>
          Vazgeç
        </Button>
        {parsed && (
          <Button onClick={reset} disabled={loading}>
            Başka PDF Seç
          </Button>
        )}
        {parsed && (
          <Button variant="contained" disabled={!canConfirm || loading} onClick={handleConfirm}>
            Siparişi Oluştur
          </Button>
        )}
      </DialogActions>
    </Dialog>
  );
}

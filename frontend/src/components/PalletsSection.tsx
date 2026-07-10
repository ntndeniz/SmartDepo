import { useCallback, useEffect, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Chip,
  Divider,
  IconButton,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import QrCodeScannerIcon from '@mui/icons-material/QrCodeScanner';
import InventoryIcon from '@mui/icons-material/Inventory';
import DeleteOutlineIcon from '@mui/icons-material/Delete';
import type { DispatchPallet, UnpalletizedBox } from '../types';
import { getErrorMessage } from '../api/client';
import { dispatchApi } from '../api/services';
import BarcodeScannerModal from './BarcodeScannerModal';
import PrintBarcodeButton from './PrintBarcodeButton';
import { formatDate } from '../utils/formatDate';

interface Props {
  companyId: string;
  refreshKey: number;
}

const statusInfo = (status: string): { label: string; color: 'default' | 'warning' | 'success' } => {
  if (status === 'Ready') return { label: 'Sevke Hazır', color: 'warning' };
  if (status === 'Shipped') return { label: 'Sevk Edildi', color: 'success' };
  return { label: 'Hazırlanıyor', color: 'default' };
};

// Paletler: bir palete yalnızca AYNI mağazanın kolileri eklenebilir. Bir koli kapatıldığında
// (Sevkiyat toplama akışında) burada "Paletlenmemiş Koliler" listesinin en üstünde hemen görünür;
// palete eklendiğinde listeden çıkar. Palet "Hazırlanıyor" durumundayken koli eklenip çıkarılabilir;
// kullanıcı "Sevkiyata Hazır" onayı verince koli listesi kilitlenir, sonra "Sevk Et" ile kapanır.
export default function PalletsSection({ companyId, refreshKey }: Props) {
  const [unpalletized, setUnpalletized] = useState<UnpalletizedBox[]>([]);
  const [pallets, setPallets] = useState<DispatchPallet[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ text: string; error: boolean } | null>(null);

  const [selectedBarcodes, setSelectedBarcodes] = useState<string[]>([]);
  const [scanInput, setScanInput] = useState('');
  const [scannerOpen, setScannerOpen] = useState(false);
  const [expandedPalletId, setExpandedPalletId] = useState<number | null>(null);
  const [addBoxInputs, setAddBoxInputs] = useState<Record<number, string>>({});
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');

  const createdBy = () => sessionStorage.getItem('username') || '';

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [boxesRes, palletsRes] = await Promise.all([
        dispatchApi.listUnpalletizedBoxes(companyId),
        dispatchApi.listPallets({
          companyId,
          page: 1,
          pageSize: 50,
          fromDate: fromDate || undefined,
          toDate: toDate || undefined,
        }),
      ]);
      if (boxesRes.data.success && boxesRes.data.data) setUnpalletized(boxesRes.data.data);
      setPallets(palletsRes.data);
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setLoading(false);
    }
  }, [companyId, fromDate, toDate]);

  useEffect(() => {
    load();
  }, [load, refreshKey]);

  const selectedStoreId = selectedBarcodes.length
    ? unpalletized.find((b) => b.barcode === selectedBarcodes[0])?.storeId ?? null
    : null;

  const toggleSelect = (box: UnpalletizedBox) => {
    setSelectedBarcodes((prev) => {
      if (prev.includes(box.barcode)) return prev.filter((b) => b !== box.barcode);
      if (selectedStoreId && box.storeId !== selectedStoreId) {
        setMessage({
          text: 'Bir palete yalnızca aynı mağazanın kolileri eklenebilir. Önce seçimi temizleyin.',
          error: true,
        });
        return prev;
      }
      return [...prev, box.barcode];
    });
  };

  const registerScan = (barcode: string) => {
    const trimmed = barcode.trim();
    if (!trimmed) return;
    const box = unpalletized.find((b) => b.barcode === trimmed);
    if (!box) {
      setMessage({ text: `${trimmed} paletlenmemiş koliler listesinde yok.`, error: true });
      return;
    }
    if (selectedStoreId && box.storeId !== selectedStoreId) {
      setMessage({
        text: 'Bir palete yalnızca aynı mağazanın kolileri eklenebilir. Önce seçimi temizleyin.',
        error: true,
      });
      return;
    }
    if (!selectedBarcodes.includes(trimmed)) {
      setSelectedBarcodes((prev) => [...prev, trimmed]);
    }
    setScanInput('');
  };

  const handleCreatePallet = async () => {
    if (selectedBarcodes.length === 0) return;
    setSaving(true);
    try {
      const res = await dispatchApi.createPallet({
        companyId,
        createdBy: createdBy(),
        boxBarcodes: selectedBarcodes,
      });
      if (res.data.success && res.data.data) {
        setMessage({ text: res.data.message || 'Palet oluşturuldu.', error: false });
        setSelectedBarcodes([]);
        await load();
      } else {
        setMessage({ text: res.data.message || 'Hata oluştu', error: true });
      }
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const handleAddBox = async (palletId: number) => {
    const barcode = (addBoxInputs[palletId] || '').trim();
    if (!barcode) return;
    setSaving(true);
    try {
      const res = await dispatchApi.addBoxToPallet({ companyId, palletId, boxBarcode: barcode });
      if (res.data.success) {
        setMessage({ text: res.data.message || 'Koli palete eklendi.', error: false });
        setAddBoxInputs((prev) => ({ ...prev, [palletId]: '' }));
        await load();
      } else {
        setMessage({ text: res.data.message || 'Hata oluştu', error: true });
      }
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const handleRemoveBox = async (palletId: number, boxBarcode: string) => {
    setSaving(true);
    try {
      const res = await dispatchApi.removeBoxFromPallet({ companyId, palletId, boxBarcode });
      if (res.data.success) {
        setMessage({ text: res.data.message || 'Koli paletten çıkarıldı.', error: false });
        await load();
      } else {
        setMessage({ text: res.data.message || 'Hata oluştu', error: true });
      }
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const handleMarkReady = async (palletId: number) => {
    setSaving(true);
    try {
      const res = await dispatchApi.markPalletReady({ companyId, palletId });
      if (res.data.success) {
        setMessage({ text: res.data.message || 'Palet sevkiyata hazır olarak onaylandı.', error: false });
        await load();
      } else {
        setMessage({ text: res.data.message || 'Hata oluştu', error: true });
      }
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  const handleMarkShipped = async (palletId: number) => {
    setSaving(true);
    try {
      const res = await dispatchApi.markPalletShipped({ companyId, palletId });
      if (res.data.success) {
        setMessage({ text: res.data.message || 'Palet sevk edildi olarak işaretlendi.', error: false });
        await load();
      } else {
        setMessage({ text: res.data.message || 'Hata oluştu', error: true });
      }
    } catch (err) {
      setMessage({ text: getErrorMessage(err), error: true });
    } finally {
      setSaving(false);
    }
  };

  return (
    <Box sx={{ mt: 4 }}>
      <Divider sx={{ mb: 3 }}>
        <Chip icon={<InventoryIcon />} label="Paletler" />
      </Divider>

      {message && (
        <Alert severity={message.error ? 'error' : 'success'} sx={{ mb: 2 }} onClose={() => setMessage(null)}>
          {message.text}
        </Alert>
      )}

      <Typography variant="subtitle1" sx={{ mb: 1 }}>
        Paletlenmemiş Koliler ({unpalletized.length})
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Kapatılan her sevkiyat kolisi burada, en yeni en üstte olacak şekilde belirir. Bir palete
        yalnızca AYNI mağazaya ait koliler eklenebilir; kaç koli koyacağınıza siz karar verirsiniz.
      </Typography>

      <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
        <TextField
          size="small"
          label="Koli Barkodu Okut"
          value={scanInput}
          onChange={(e) => setScanInput(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter') registerScan(scanInput);
          }}
          sx={{ flex: 1 }}
        />
        <IconButton onClick={() => setScannerOpen(true)} title="Kamera ile tara">
          <QrCodeScannerIcon />
        </IconButton>
        <Button
          variant="contained"
          disabled={selectedBarcodes.length === 0 || saving}
          onClick={handleCreatePallet}
        >
          {selectedBarcodes.length > 0 ? `Palet Oluştur (${selectedBarcodes.length} koli)` : 'Palet Oluştur'}
        </Button>
      </Stack>

      {unpalletized.length === 0 ? (
        <Typography color="text.secondary" variant="body2" sx={{ mb: 3 }}>
          {loading ? 'Yükleniyor...' : 'Paletlenmeyi bekleyen koli yok.'}
        </Typography>
      ) : (
        <Table size="small" sx={{ mb: 4 }}>
          <TableHead>
            <TableRow>
              <TableCell />
              <TableCell>Koli Barkodu</TableCell>
              <TableCell>Mağaza</TableCell>
              <TableCell>İçerik</TableCell>
              <TableCell>Oluşturulma</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {unpalletized.map((box) => {
              const disabled = selectedStoreId !== null && box.storeId !== selectedStoreId;
              return (
                <TableRow
                  key={box.id}
                  hover
                  selected={selectedBarcodes.includes(box.barcode)}
                  onClick={() => (disabled ? undefined : toggleSelect(box))}
                  sx={{ cursor: disabled ? 'not-allowed' : 'pointer', opacity: disabled ? 0.4 : 1 }}
                >
                  <TableCell padding="checkbox">
                    <input
                      type="checkbox"
                      checked={selectedBarcodes.includes(box.barcode)}
                      disabled={disabled}
                      readOnly
                    />
                  </TableCell>
                  <TableCell>{box.barcode}</TableCell>
                  <TableCell>
                    {box.storeName} ({box.storeId})
                  </TableCell>
                  <TableCell>{box.itemsSummary}</TableCell>
                  <TableCell>{formatDate(box.createdAt)}</TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      )}

      <Stack direction="row" spacing={1} sx={{ mb: 1, alignItems: 'center', flexWrap: 'wrap' }}>
        <Typography variant="subtitle1">Oluşturulan Paletler ({pallets.length})</Typography>
        <Box sx={{ flexGrow: 1 }} />
        <TextField
          size="small"
          type="date"
          label="Başlangıç"
          value={fromDate}
          onChange={(e) => setFromDate(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          size="small"
          type="date"
          label="Bitiş"
          value={toDate}
          onChange={(e) => setToDate(e.target.value)}
          slotProps={{ inputLabel: { shrink: true } }}
        />
        {(fromDate || toDate) && (
          <Button
            size="small"
            onClick={() => {
              setFromDate('');
              setToDate('');
            }}
          >
            Filtreyi Temizle
          </Button>
        )}
      </Stack>
      {pallets.length === 0 ? (
        <Typography color="text.secondary" variant="body2">
          Henüz palet oluşturulmadı.
        </Typography>
      ) : (
        <Stack spacing={1}>
          {pallets.map((pallet) => {
            const info = statusInfo(pallet.status);
            const editable = pallet.status === 'Preparing';
            return (
              <Paper key={pallet.id} sx={{ p: 1.5 }}>
                <Stack
                  direction="row"
                  spacing={1}
                  sx={{ alignItems: 'center', justifyContent: 'space-between', cursor: 'pointer' }}
                  onClick={() => setExpandedPalletId((id) => (id === pallet.id ? null : pallet.id))}
                >
                  <Stack>
                    <Typography variant="subtitle2">📦 {pallet.barcode}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {pallet.storeName} ({pallet.storeId}) — {formatDate(pallet.createdAt)}
                    </Typography>
                  </Stack>
                  <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                    <Chip size="small" label={info.label} color={info.color} />
                    <Chip size="small" label={`${pallet.boxCount} koli`} />
                    <Chip size="small" label={`${pallet.totalItemQuantity} ürün`} />
                    <PrintBarcodeButton value={pallet.barcode} label="Sevkiyat Paleti" />
                  </Stack>
                </Stack>
                {expandedPalletId === pallet.id && (
                  <Box sx={{ mt: 1, pl: 2 }} onClick={(e) => e.stopPropagation()}>
                    {pallet.boxBarcodes.map((barcode) => (
                      <Stack
                        key={barcode}
                        direction="row"
                        spacing={1}
                        sx={{ alignItems: 'center' }}
                      >
                        <Typography variant="caption" sx={{ display: 'block' }}>
                          • {barcode}
                        </Typography>
                        {editable && (
                          <IconButton
                            size="small"
                            disabled={saving}
                            onClick={() => handleRemoveBox(pallet.id, barcode)}
                            title="Paletten çıkar"
                          >
                            <DeleteOutlineIcon fontSize="inherit" />
                          </IconButton>
                        )}
                      </Stack>
                    ))}

                    {editable && (
                      <Stack direction="row" spacing={1} sx={{ mt: 1, alignItems: 'center' }}>
                        <TextField
                          size="small"
                          label="Koli Barkodu Ekle"
                          value={addBoxInputs[pallet.id] || ''}
                          onChange={(e) =>
                            setAddBoxInputs((prev) => ({ ...prev, [pallet.id]: e.target.value }))
                          }
                          onKeyDown={(e) => {
                            if (e.key === 'Enter') handleAddBox(pallet.id);
                          }}
                        />
                        <Button
                          size="small"
                          variant="outlined"
                          disabled={saving || !(addBoxInputs[pallet.id] || '').trim()}
                          onClick={() => handleAddBox(pallet.id)}
                        >
                          Ekle
                        </Button>
                      </Stack>
                    )}

                    <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
                      {pallet.status === 'Preparing' && (
                        <Button
                          size="small"
                          variant="contained"
                          color="warning"
                          disabled={saving || pallet.boxCount === 0}
                          onClick={() => handleMarkReady(pallet.id)}
                        >
                          Sevkiyata Hazır Onayla
                        </Button>
                      )}
                      {pallet.status === 'Ready' && (
                        <Button
                          size="small"
                          variant="contained"
                          color="success"
                          disabled={saving}
                          onClick={() => handleMarkShipped(pallet.id)}
                        >
                          Sevk Et
                        </Button>
                      )}
                      {pallet.status === 'Shipped' && (
                        <Typography variant="caption" color="success.main">
                          Bu palet sevk edildi.
                        </Typography>
                      )}
                    </Stack>
                  </Box>
                )}
              </Paper>
            );
          })}
        </Stack>
      )}

      <BarcodeScannerModal
        open={scannerOpen}
        onClose={() => setScannerOpen(false)}
        onScan={(barcode) => {
          setScannerOpen(false);
          registerScan(barcode);
        }}
      />
    </Box>
  );
}

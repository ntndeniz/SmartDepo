import { useEffect, useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  TextField,
  Typography,
  Paper,
  Chip,
} from '@mui/material';
import QrCodeScannerIcon from '@mui/icons-material/QrCodeScanner';
import type { GoodsReceiptItem } from '../types';
import { getErrorMessage } from '../api/client';
import { goodsReceiptApi, productApi } from '../api/services';
import BarcodeScannerModal from './BarcodeScannerModal';
import PrintBarcodeButton from './PrintBarcodeButton';
import { formatDate } from '../utils/formatDate';

interface Props {
  open: boolean;
  companyId: string;
  loading?: boolean;
  onSave: () => void;
  onClose: () => void;
}

type Step = 'start' | 'scan-product' | 'enter-desi' | 'review';

export default function GoodsReceiptFormModal({
  open,
  companyId,
  loading,
  onSave,
  onClose,
}: Props) {
  const [step, setStep] = useState<Step>('start');
  const [receiptId, setReceiptId] = useState<number | null>(null);
  const [items, setItems] = useState<GoodsReceiptItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Tarama adımı: barkod tekrar tekrar okutuldukça kalem sayısı otomatik artar
  const [scanInput, setScanInput] = useState('');
  const [scannedBarcode, setScannedBarcode] = useState<string | null>(null);
  const [scannedProductName, setScannedProductName] = useState<string | null>(null);
  const [scanCount, setScanCount] = useState(0);
  const [desi, setDesi] = useState<number | null>(null);
  const [scannerActive, setScannerActive] = useState(false);
  const [checkingBarcode, setCheckingBarcode] = useState(false);

  useEffect(() => {
    if (!open) {
      setStep('start');
      setReceiptId(null);
      setItems([]);
      setError(null);
      setSuccess(null);
      setScanInput('');
      setScannedBarcode(null);
      setScannedProductName(null);
      setScanCount(0);
      setDesi(null);
    }
  }, [open]);

  const handleStartSession = async () => {
    try {
      setError(null);
      const res = await goodsReceiptApi.createSession({ companyId });
      if (res.data.success && res.data.data) {
        setReceiptId(res.data.data.id);
        setItems([]);
        setStep('scan-product');
        setSuccess('Mal kabul oturumu açıldı');
      } else {
        setError(res.data.message || 'Oturum açılamadı');
      }
    } catch (e) {
      setError(`Oturum açılırken hata: ${e}`);
    }
  };

  const registerScan = async (barcode: string) => {
    const trimmed = barcode.trim();
    if (!trimmed) return;

    if (scannedBarcode !== null && trimmed !== scannedBarcode) {
      setError(
        'Farklı bir ürün barkodu okutuldu. Önce mevcut ürünü onaylayın ya da "Farklı Ürün" ile sıfırlayın.'
      );
      setScanInput('');
      return;
    }

    if (scannedBarcode !== null && trimmed === scannedBarcode) {
      setScanCount((c) => c + 1);
      setError(null);
      setScanInput('');
      return;
    }

    // İlk tarama: sistemde tanımlı bir ürün mü diye hemen doğrula — tanımsız barkodu
    // sayaça hiç eklemeden, en baştan reddet (daha önce yalnızca "Kabul Et" adımında reddediliyordu).
    // NOT: tanımsız barkod backend'den 404 döndüğü için axios her zaman reject eder — bu yüzden hata
    // mesajı yalnızca catch bloğunda (backend'in kendi mesajıyla) gösterilir.
    setCheckingBarcode(true);
    setError(null);
    try {
      const res = await productApi.getByBarcode(companyId, trimmed);
      if (res.data.success && res.data.data) {
        setScannedBarcode(trimmed);
        setScannedProductName(res.data.data.name);
        setScanCount(1);
      }
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setCheckingBarcode(false);
      setScanInput('');
    }
  };

  const handleResetScan = () => {
    setScannedBarcode(null);
    setScannedProductName(null);
    setScanCount(0);
    setScanInput('');
    setError(null);
  };

  const handleScanItem = async () => {
    if (!receiptId || !scannedBarcode || scanCount <= 0) return;

    const createdBy = sessionStorage.getItem('username') || '';

    try {
      setError(null);
      const res = await goodsReceiptApi.scanItem({
        companyId,
        GoodsReceiptId: receiptId,
        productBarcode: scannedBarcode,
        quantity: scanCount,
        desi,
        createdBy,
      });

      if (res.data.success && res.data.data) {
        setItems(res.data.data.items);
        const createdBoxBarcode = res.data.data.items[res.data.data.items.length - 1]?.boxBarcode;
        setSuccess(`${scannedBarcode} ürünü kabul edildi (${scanCount} adet). Koli Barkodu: ${createdBoxBarcode}`);
        // Sıfırla ve sonraki tarama
        handleResetScan();
        setDesi(null);
        setStep('scan-product');
      } else {
        setError(res.data.message || 'Tarama başarısız');
      }
    } catch (e) {
      setError(`Tarama hatası: ${e}`);
    }
  };

  const handleFinish = () => {
    onSave();
    onClose();
  };

  return (
    <>
      <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
        <DialogTitle>Mal Kabul Sihirbazı</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            {error && <Alert severity="error">{error}</Alert>}
            {success && <Alert severity="success">{success}</Alert>}

            {/* START */}
            {step === 'start' && (
              <Stack spacing={2}>
                <Typography variant="body2">
                  Yeni bir mal kabul oturumu başlatmak için aşağıdaki düğmeye tıklayın.
                </Typography>
              </Stack>
            )}

            {/* SCAN PRODUCT (repeated scans increment count) */}
            {step === 'scan-product' && (
              <Stack spacing={2}>
                <Typography variant="subtitle2">
                  Ürün barkodunu her okuttuğunuzda/girdiğinizde Kalem sayısı otomatik artar.
                </Typography>
                <TextField
                  label="Ürün Barkodu Okut"
                  value={scanInput}
                  onChange={(e) => setScanInput(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') {
                      e.preventDefault();
                      registerScan(scanInput);
                    }
                  }}
                  disabled={checkingBarcode}
                  autoFocus
                  fullWidth
                  placeholder="Barkodu okutun veya yazıp Enter'a basın..."
                  helperText={checkingBarcode ? 'Ürün doğrulanıyor...' : undefined}
                />
                <Button
                  startIcon={<QrCodeScannerIcon />}
                  onClick={() => setScannerActive(true)}
                  variant="outlined"
                  disabled={checkingBarcode}
                >
                  Kamera ile Tara
                </Button>

                {scannedBarcode && (
                  <Paper sx={{ p: 2, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <Stack spacing={0.5}>
                      <Typography variant="subtitle2">
                        Taranan Ürün: {scannedProductName ?? ''} ({scannedBarcode})
                      </Typography>
                      <Typography variant="body2" color="textSecondary">
                        Kalem (Miktar): {scanCount}
                      </Typography>
                    </Stack>
                    <Button size="small" onClick={handleResetScan}>
                      Farklı Ürün
                    </Button>
                  </Paper>
                )}
              </Stack>
            )}

            {/* ENTER DESI */}
            {step === 'enter-desi' && (
              <Stack spacing={2}>
                <Typography variant="subtitle2">
                  {scannedBarcode} — Kalem: {scanCount} — Desi (ağırlık, opsiyonel)
                </Typography>
                <TextField
                  label="Desi (decimal, boş bırakılabilir)"
                  type="number"
                  value={desi ?? ''}
                  onChange={(e) => setDesi(e.target.value ? parseFloat(e.target.value) : null)}
                  slotProps={{ htmlInput: { step: '0.01' } }}
                  fullWidth
                />
                <Button variant="contained" onClick={handleScanItem}>
                  Kabul Et (Koli Otomatik Oluşturulacak)
                </Button>
              </Stack>
            )}

            {/* REVIEW */}
            {step === 'review' && (
              <Stack spacing={2}>
                <Typography variant="subtitle2">
                  Kabul Edilen Kalemler ({items.length})
                </Typography>
                {items.length === 0 ? (
                  <Typography variant="body2" color="textSecondary">
                    Henüz kalem eklenmedi.
                  </Typography>
                ) : (
                  items.map((item) => (
                    <Paper
                      key={item.id}
                      sx={{ p: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}
                    >
                      <Stack spacing={0.5}>
                        <Typography variant="subtitle2">
                          {item.productName} ({item.productBarcode})
                        </Typography>
                        <Typography variant="body2" color="textSecondary">
                          Marka: {item.brandName} | Renk: {item.productColor} | Koli: {item.boxBarcode} | Kalem: {item.countedQuantity}
                          {item.desi !== null && ` | Desi: ${item.desi}`}
                        </Typography>
                        <Typography variant="caption" color="textSecondary">
                          İşlem Tarihi: {formatDate(item.createdAt)}
                        </Typography>
                      </Stack>
                      <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                        <Chip size="small" label={`Kalem: ${item.countedQuantity}`} />
                        <PrintBarcodeButton value={item.boxBarcode} label={item.productName} />
                      </Stack>
                    </Paper>
                  ))
                )}
              </Stack>
            )}
          </Stack>
        </DialogContent>

        <DialogActions>
          <Button
            onClick={() => {
              if (step === 'enter-desi') {
                setStep('scan-product');
              } else {
                onClose();
              }
            }}
            disabled={loading}
          >
            {step === 'enter-desi' ? 'Geri' : 'Kapat'}
          </Button>
          {step === 'start' && (
            <Button
              variant="contained"
              disabled={loading}
              onClick={handleStartSession}
            >
              Oturum Başlat
            </Button>
          )}
          {step === 'scan-product' && (
            <>
              {items.length > 0 && (
                <Button onClick={() => setStep('review')} disabled={loading}>
                  Kalemleri Gör ({items.length})
                </Button>
              )}
              <Button
                variant="contained"
                disabled={loading || !scannedBarcode || scanCount <= 0}
                onClick={() => setStep('enter-desi')}
              >
                Devam (Desi)
              </Button>
            </>
          )}
          {step === 'review' && (
            <>
              <Button onClick={() => setStep('scan-product')} disabled={loading}>
                Taramaya Devam Et
              </Button>
              <Button variant="contained" disabled={loading} onClick={handleFinish}>
                Tamamla
              </Button>
            </>
          )}
        </DialogActions>
      </Dialog>

      <BarcodeScannerModal
        open={scannerActive}
        onClose={() => setScannerActive(false)}
        onScan={(barcode) => {
          setScannerActive(false);
          registerScan(barcode);
        }}
      />
    </>
  );
}

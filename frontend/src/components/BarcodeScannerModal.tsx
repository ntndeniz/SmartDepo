import { useEffect, useRef, useState } from 'react';
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
} from '@mui/material';
import { Html5Qrcode } from 'html5-qrcode';

interface Props {
  open: boolean;
  onClose: () => void;
  onScan: (barcode: string) => void;
}

const READER_ID = 'barcode-scanner-region';

// Web kamerasından barkod/QR okur, çözülen metni forma geri verir.
// Görüntü backend'e gönderilmez; çözümleme tamamen tarayıcıda yapılır.
export default function BarcodeScannerModal({ open, onClose, onScan }: Props) {
  const scannerRef = useRef<Html5Qrcode | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;

    let cancelled = false;
    setError(null);

    const start = async () => {
      // Dialog içeriğinin DOM'a yerleşmesini bekle
      await new Promise((resolve) => setTimeout(resolve, 300));
      if (cancelled) return;

      try {
        const scanner = new Html5Qrcode(READER_ID);
        scannerRef.current = scanner;
        await scanner.start(
          { facingMode: 'environment' },
          { fps: 10, qrbox: { width: 250, height: 150 } },
          (decodedText) => {
            onScan(decodedText);
            void stop();
            onClose();
          },
          () => {
            // tek kare çözülemedi; taramaya devam
          }
        );
      } catch (err) {
        if (!cancelled) {
          setError(
            'Kamera başlatılamadı. Tarayıcı izinlerini kontrol edin: ' +
              (err instanceof Error ? err.message : String(err))
          );
        }
      }
    };

    const stop = async () => {
      const scanner = scannerRef.current;
      scannerRef.current = null;
      if (scanner) {
        try {
          if (scanner.isScanning) {
            await scanner.stop();
          }
          scanner.clear();
        } catch {
          // kamera zaten kapalı
        }
      }
    };

    void start();

    return () => {
      cancelled = true;
      void stop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Barkod Tara</DialogTitle>
      <DialogContent>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        <Box
          id={READER_ID}
          sx={{ width: '100%', minHeight: 300, '& video': { width: '100%' } }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Kapat</Button>
      </DialogActions>
    </Dialog>
  );
}

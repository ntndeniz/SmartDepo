import { useRef, useState } from 'react';
import { IconButton, Snackbar, Alert, Tooltip } from '@mui/material';
import PrintIcon from '@mui/icons-material/Print';
import JsBarcode from 'jsbarcode';

interface Props {
  value: string;
  label?: string;
  size?: 'small' | 'medium';
}

export default function PrintBarcodeButton({ value, label, size = 'small' }: Props) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handlePrint = () => {
    if (!value) return;

    if (!canvasRef.current) {
      canvasRef.current = document.createElement('canvas');
    }

    // JsBarcode CODE128'de geçersiz karakter (ör. eski verilerde kalmış Türkçe İ/ı) varsa istisna
    // fırlatır — bunu yakalamazsak yazdırma sessizce hiçbir şey yapmadan başarısız olur.
    try {
      JsBarcode(canvasRef.current, value, {
        format: 'CODE128',
        displayValue: true,
        fontSize: 18,
        height: 70,
        margin: 10,
      });
    } catch {
      setError(`"${value}" barkodu yazdırılamıyor (geçersiz karakter içeriyor). Lütfen destek ile iletişime geçin.`);
      return;
    }

    const dataUrl = canvasRef.current.toDataURL('image/png');

    const printWindow = window.open('', '_blank', 'width=420,height=300');
    if (!printWindow) {
      setError('Yazdırma penceresi açılamadı — tarayıcınızın popup engelleyicisini bu site için kapatın.');
      return;
    }

    printWindow.document.write(`
      <html>
        <head>
          <title>Barkod Yazdır — ${value}</title>
          <style>
            body { margin: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; height: 100vh; font-family: sans-serif; }
            img { max-width: 100%; }
            p { margin: 4px 0 0; font-size: 14px; color: #333; }
          </style>
        </head>
        <body>
          <img src="${dataUrl}" alt="${value}" />
          ${label ? `<p>${label}</p>` : ''}
        </body>
      </html>
    `);
    printWindow.document.close();
    printWindow.focus();
    printWindow.onload = () => {
      printWindow.print();
    };
    // Bazı tarayıcılarda onload tetiklenmeyebilir, kısa gecikmeyle de deneriz
    setTimeout(() => {
      printWindow.print();
    }, 300);
  };

  return (
    <>
      <Tooltip title="Barkodu Yazdır">
        <IconButton size={size} onClick={handlePrint} disabled={!value}>
          <PrintIcon fontSize={size} />
        </IconButton>
      </Tooltip>
      <Snackbar
        open={error !== null}
        autoHideDuration={5000}
        onClose={() => setError(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert severity="error" onClose={() => setError(null)}>
          {error}
        </Alert>
      </Snackbar>
    </>
  );
}

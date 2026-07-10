import { useState } from 'react';
import {
  Box,
  Card,
  CardContent,
  CardHeader,
  TextField,
  Button,
  Alert,
  CircularProgress,
  Container,
} from '@mui/material';
import WarehouseIcon from '@mui/icons-material/Warehouse';
import { authApi } from '../api/services';
import { getErrorMessage } from '../api/client';

interface Props {
  onLoginSuccess: (companyId: string) => void;
}

export default function LoginScreen({ onLoginSuccess }: Props) {
  const [companyId, setCompanyId] = useState('');
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleLogin = async () => {
    setError(null);
    setLoading(true);

    try {
      const res = await authApi.login({ companyId, username, password });

      if (res.data.success && res.data.data) {
        sessionStorage.setItem('token', res.data.data.token);
        sessionStorage.setItem('companyId', companyId);
        sessionStorage.setItem('username', res.data.data.user.username);
        sessionStorage.setItem('role', res.data.data.user.role);
        sessionStorage.setItem('isLoggedIn', 'true');
        onLoginSuccess(companyId);
      } else {
        setError(res.data.message || 'Giriş başarısız.');
      }
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && companyId && username && password) {
      handleLogin();
    }
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        bgcolor: '#f5f6fa',
      }}
    >
      <Container maxWidth="sm">
        <Card sx={{ boxShadow: 3 }}>
          <CardHeader
            avatar={<WarehouseIcon sx={{ fontSize: 40, color: '#1976d2' }} />}
            title="Akıllı Depo Yönetimi"
            subheader="Şirkete Giriş"
            sx={{ textAlign: 'center', pb: 1 }}
          />
          <CardContent sx={{ pt: 3 }}>
            {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

            <TextField
              fullWidth
              label="Şirket Adı (CompanyId)"
              value={companyId}
              onChange={(e) => setCompanyId(e.target.value)}
              onKeyPress={handleKeyPress}
              disabled={loading}
              margin="normal"
              placeholder="Örn: demo-sirket"
              autoFocus
            />

            <TextField
              fullWidth
              label="Kullanıcı Adı"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              onKeyPress={handleKeyPress}
              disabled={loading}
              margin="normal"
              placeholder="Kullanıcı adınız"
            />

            <TextField
              fullWidth
              type="password"
              label="Kullanıcı Şifresi"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onKeyPress={handleKeyPress}
              disabled={loading}
              margin="normal"
              placeholder="Şifre girin"
            />

            <Button
              fullWidth
              variant="contained"
              size="large"
              disabled={!companyId || !username || !password || loading}
              onClick={handleLogin}
              sx={{ mt: 3 }}
            >
              {loading ? <CircularProgress size={24} /> : 'Giriş Yap'}
            </Button>

            <Alert severity="info" sx={{ mt: 3 }}>
              Test: CompanyId = "demo-sirket", Kullanıcı Adı = "admin", Şifre = "123"
            </Alert>
          </CardContent>
        </Card>
      </Container>
    </Box>
  );
}

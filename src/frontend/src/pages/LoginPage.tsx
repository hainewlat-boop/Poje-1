import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { authApi } from '../services/api';
import { getFingerprint } from '../services/fingerprint';

export default function LoginPage() {
  const navigate = useNavigate();
  const { setCredentials, setMfaRequired, setFingerprintId, isAuthenticated } = useAuthStore();
  
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [fingerprintId, setLocalFingerprintId] = useState<string | null>(null);

  useEffect(() => {
    if (isAuthenticated) {
      navigate('/');
    }
  }, [isAuthenticated, navigate]);

  useEffect(() => {
    // Get fingerprint on mount
    getFingerprint().then((fp) => {
      setLocalFingerprintId(fp);
      setFingerprintId(fp);
    });
  }, [setFingerprintId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    if (!fingerprintId) {
      setError('Tarayıcı parmak izi alınamadı. Lütfen sayfayı yenileyin.');
      setLoading(false);
      return;
    }

    try {
      const response = await authApi.login(email, password, fingerprintId);

      if (response.requiresMfa) {
        setMfaRequired(response.mfaToken);
        navigate('/mfa');
      } else {
        setCredentials({
          accessToken: response.accessToken,
          refreshToken: response.refreshToken,
        });
        navigate('/');
      }
    } catch (err: any) {
      const message = err.response?.data?.detail || 'Giriş başarısız. Lütfen bilgilerinizi kontrol edin.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-government-600 to-government-800 flex items-center justify-center p-4">
      <div className="card w-full max-w-md">
        <div className="text-center mb-8">
          <div className="w-16 h-16 bg-government-600 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg className="w-8 h-8 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
          </div>
          <h1 className="text-2xl font-bold text-gray-900">
            Kurumsal Platform
          </h1>
          <p className="text-gray-600 mt-2">
            Başvuru ve Ödeme Yönetim Sistemi
          </p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {error && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
              {error}
            </div>
          )}

          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
              E-posta
            </label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="input-field"
              placeholder="ornek@kurum.gov.tr"
              required
              autoComplete="email"
            />
          </div>

          <div>
            <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
              Şifre
            </label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="input-field"
              placeholder="••••••••"
              required
              autoComplete="current-password"
            />
          </div>

          <button
            type="submit"
            disabled={loading || !fingerprintId}
            className="btn-primary w-full flex items-center justify-center"
          >
            {loading ? (
              <>
                <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                Giriş yapılıyor...
              </>
            ) : (
              'Giriş Yap'
            )}
          </button>
        </form>

        <div className="mt-6 pt-6 border-t border-gray-200">
          <p className="text-xs text-gray-500 text-center">
            Bu sistem resmi devlet uygulamasıdır. Yetkisiz erişim yasaktır.
            Tüm işlemler kayıt altına alınmaktadır.
          </p>
        </div>
      </div>
    </div>
  );
}

import { FormEvent, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { KeyRound } from 'lucide-react';
import { api } from '../api/client';
import TopBar from '../components/TopBar';

export default function ResetPassword() {
  const { t } = useTranslation();
  const [params] = useSearchParams();
  const [password, setPassword] = useState('');
  const [done, setDone] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await api.post('/api/auth/reset-password', {
        email: params.get('email') ?? '',
        token: params.get('token') ?? '',
        newPassword: password,
      });
      setDone(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <TopBar />
      <div className="card auth-card">
        <h2>{t('auth.resetPassword')}</h2>
        {done ? (
          <>
            <div className="success-box">{t('auth.passwordResetDone')}</div>
            <Link to="/login" className="btn" style={{ width: '100%' }}>
              {t('auth.login')}
            </Link>
          </>
        ) : (
          <>
            {error && <div className="error-box">{error}</div>}
            <form onSubmit={submit}>
              <div className="field">
                <label>{t('auth.newPassword')}</label>
                <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={8} autoFocus />
              </div>
              <button className="btn" type="submit" disabled={busy} style={{ width: '100%' }}>
                <KeyRound size={16} />
                {t('auth.resetPassword')}
              </button>
            </form>
          </>
        )}
      </div>
    </>
  );
}

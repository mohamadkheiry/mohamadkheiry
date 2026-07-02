import { FormEvent, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { UserPlus } from 'lucide-react';
import { api } from '../api/client';
import type { AuthResult } from '../api/types';
import { useAuth } from '../store/auth';
import TopBar from '../components/TopBar';

export default function Register() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      const result = await api.post<AuthResult>('/api/auth/register', { email, password, displayName });
      login(result);
      navigate('/dashboard');
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
        <h2>{t('auth.register')}</h2>
        {error && <div className="error-box">{error}</div>}
        <form onSubmit={submit}>
          <div className="field">
            <label>{t('auth.displayName')}</label>
            <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required autoFocus />
          </div>
          <div className="field">
            <label>{t('auth.email')}</label>
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </div>
          <div className="field">
            <label>{t('auth.password')}</label>
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={8} />
          </div>
          <button className="btn" type="submit" disabled={busy} style={{ width: '100%' }}>
            <UserPlus size={16} />
            {t('auth.register')}
          </button>
        </form>
        <p style={{ marginTop: 16, fontSize: 14, color: 'var(--text-dim)' }}>
          {t('auth.haveAccount')} <Link to="/login">{t('auth.login')}</Link>
        </p>
      </div>
    </>
  );
}

import { FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Mail } from 'lucide-react';
import { api } from '../api/client';
import TopBar from '../components/TopBar';

export default function ForgotPassword() {
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [sent, setSent] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await api.post('/api/auth/forgot-password', { email });
      setSent(true);
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
        {sent && <div className="success-box">{t('auth.resetLinkSent')}</div>}
        {error && <div className="error-box">{error}</div>}
        <form onSubmit={submit}>
          <div className="field">
            <label>{t('auth.email')}</label>
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoFocus />
          </div>
          <button className="btn" type="submit" disabled={busy} style={{ width: '100%' }}>
            <Mail size={16} />
            {t('auth.sendResetLink')}
          </button>
        </form>
      </div>
    </>
  );
}

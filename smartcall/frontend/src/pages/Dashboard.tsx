import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Phone, Copy, Check, ArrowLeft } from 'lucide-react';
import { api } from '../api/client';
import TopBar from '../components/TopBar';

export default function Dashboard() {
  const { t } = useTranslation();
  const [linkCode, setLinkCode] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState('');

  const createCall = async () => {
    setBusy(true);
    setError('');
    try {
      const result = await api.post<{ callId: string; linkCode: string }>('/api/calls');
      setLinkCode(result.linkCode);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    } finally {
      setBusy(false);
    }
  };

  const inviteUrl = linkCode ? `${window.location.origin}/call/${linkCode}` : '';

  const copy = async () => {
    await navigator.clipboard.writeText(inviteUrl);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <>
      <TopBar />
      <main className="container" style={{ paddingTop: 40 }}>
        <h1>{t('dashboard.title')}</h1>
        <div className="card" style={{ maxWidth: 620 }}>
          {error && <div className="error-box">{error}</div>}
          {!linkCode ? (
            <>
              <p style={{ color: 'var(--text-dim)' }}>{t('dashboard.hint')}</p>
              <button className="btn" onClick={createCall} disabled={busy} style={{ fontSize: 17 }}>
                <Phone size={18} />
                {busy ? t('dashboard.creating') : t('dashboard.newCall')}
              </button>
            </>
          ) : (
            <>
              <div className="field">
                <label>{t('dashboard.inviteLink')}</label>
                <input readOnly value={inviteUrl} onFocus={(e) => e.target.select()} dir="ltr" />
              </div>
              <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
                <button className="btn secondary" onClick={copy}>
                  {copied ? <Check size={16} /> : <Copy size={16} />}
                  {copied ? t('dashboard.copied') : t('dashboard.copy')}
                </button>
                <Link to={`/call/${linkCode}`} className="btn">
                  <ArrowLeft size={16} />
                  {t('dashboard.joinOwn')}
                </Link>
              </div>
              <p style={{ color: 'var(--text-dim)', fontSize: 14, marginTop: 14 }}>{t('dashboard.hint')}</p>
            </>
          )}
        </div>
      </main>
    </>
  );
}

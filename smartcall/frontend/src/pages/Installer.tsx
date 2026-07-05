import { FormEvent, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Database, ShieldCheck, RefreshCw, CheckCircle2, Loader2 } from 'lucide-react';
import { api } from '../api/client';

interface Props {
  onInstalled: () => void;
}

/** WordPress-style install wizard: DB connection → test → admin account → install. */
export default function Installer({ onInstalled }: Props) {
  const { t } = useTranslation();

  const [db, setDb] = useState({ host: 'localhost', port: 5432, database: 'smartcall', username: 'postgres', password: '' });
  const [admin, setAdmin] = useState({ adminEmail: '', adminPassword: '', adminDisplayName: '' });
  const [connectionOk, setConnectionOk] = useState(false);
  const [testing, setTesting] = useState(false);
  const [installing, setInstalling] = useState(false);
  const [upgrading, setUpgrading] = useState(false);
  const [done, setDone] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const testConnection = async () => {
    setTesting(true);
    setError('');
    try {
      const result = await api.post<{ success: boolean; message: string }>('/api/install/test-connection', db);
      setConnectionOk(result.success);
      if (!result.success) setError(result.message);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    } finally {
      setTesting(false);
    }
  };

  const install = async (e: FormEvent) => {
    e.preventDefault();
    setInstalling(true);
    setError('');
    try {
      await api.post('/api/install/fresh', { db, ...admin });
      setDone(true);
      setTimeout(onInstalled, 1500);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    } finally {
      setInstalling(false);
    }
  };

  const upgrade = async () => {
    setUpgrading(true);
    setError('');
    setNotice('');
    try {
      const result = await api.post<{ message: string }>('/api/install/upgrade', db);
      setNotice(result.message);
      setTimeout(onInstalled, 1500);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    } finally {
      setUpgrading(false);
    }
  };

  if (done) {
    return (
      <div className="card auth-card" style={{ textAlign: 'center' }}>
        <CheckCircle2 size={48} color="#2ec27e" style={{ margin: '0 auto 12px' }} />
        <h2>{t('installer.done')}</h2>
      </div>
    );
  }

  return (
    <div className="container" style={{ paddingTop: 50, paddingBottom: 50, maxWidth: 620 }}>
      <h1 style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
        <Database size={28} color="#4f7cff" />
        {t('installer.title')}
      </h1>
      <p style={{ color: 'var(--text-dim)' }}>{t('installer.subtitle')}</p>

      {error && <div className="error-box">{error}</div>}
      {notice && <div className="success-box">{notice}</div>}

      <form onSubmit={install}>
        <div className="card" style={{ marginBottom: 20 }}>
          <h3>{t('installer.dbStep')}</h3>
          <div className="grid-2">
            <div className="field">
              <label>{t('installer.dbHost')}</label>
              <input value={db.host} onChange={(e) => setDb({ ...db, host: e.target.value })} required dir="ltr" />
            </div>
            <div className="field">
              <label>{t('installer.dbPort')}</label>
              <input type="number" value={db.port} onChange={(e) => setDb({ ...db, port: Number(e.target.value) })} required dir="ltr" />
            </div>
            <div className="field">
              <label>{t('installer.dbName')}</label>
              <input value={db.database} onChange={(e) => setDb({ ...db, database: e.target.value })} required dir="ltr" />
            </div>
            <div className="field">
              <label>{t('installer.dbUser')}</label>
              <input value={db.username} onChange={(e) => setDb({ ...db, username: e.target.value })} required dir="ltr" />
            </div>
          </div>
          <div className="field">
            <label>{t('installer.dbPassword')}</label>
            <input type="password" value={db.password} onChange={(e) => setDb({ ...db, password: e.target.value })} dir="ltr" />
          </div>
          <button type="button" className="btn secondary" onClick={testConnection} disabled={testing}>
            {testing ? <Loader2 size={16} className="spin" /> : <Database size={16} />}
            {t('installer.testConnection')}
          </button>
          {connectionOk && (
            <span className="badge green" style={{ marginInlineStart: 10 }}>
              {t('installer.connectionOk')}
            </span>
          )}
        </div>

        <div className="card" style={{ marginBottom: 20 }}>
          <h3>{t('installer.adminStep')}</h3>
          <div className="field">
            <label>{t('auth.displayName')}</label>
            <input value={admin.adminDisplayName} onChange={(e) => setAdmin({ ...admin, adminDisplayName: e.target.value })} required />
          </div>
          <div className="field">
            <label>{t('auth.email')}</label>
            <input type="email" value={admin.adminEmail} onChange={(e) => setAdmin({ ...admin, adminEmail: e.target.value })} required dir="ltr" />
          </div>
          <div className="field">
            <label>{t('auth.password')}</label>
            <input type="password" value={admin.adminPassword} onChange={(e) => setAdmin({ ...admin, adminPassword: e.target.value })} required minLength={8} dir="ltr" />
          </div>
        </div>

        <button className="btn" type="submit" disabled={!connectionOk || installing} style={{ width: '100%', fontSize: 16 }}>
          {installing ? <Loader2 size={18} className="spin" /> : <ShieldCheck size={18} />}
          {installing ? t('installer.installing') : t('installer.install')}
        </button>
      </form>

      <div className="card" style={{ marginTop: 26 }}>
        <h3>{t('installer.upgradeTitle')}</h3>
        <p style={{ color: 'var(--text-dim)', fontSize: 14 }}>{t('installer.upgradeText')}</p>
        <button className="btn secondary" onClick={upgrade} disabled={upgrading}>
          {upgrading ? <Loader2 size={16} className="spin" /> : <RefreshCw size={16} />}
          {t('installer.upgrade')}
        </button>
      </div>
    </div>
  );
}

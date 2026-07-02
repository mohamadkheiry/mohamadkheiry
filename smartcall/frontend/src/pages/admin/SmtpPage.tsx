import { FormEvent, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Save, Send, Loader2 } from 'lucide-react';
import { api } from '../../api/client';
import type { SmtpSettings } from '../../api/types';

export default function SmtpPage() {
  const { t } = useTranslation();
  const [settings, setSettings] = useState<SmtpSettings | null>(null);
  const [password, setPassword] = useState('');
  const [testEmail, setTestEmail] = useState('');
  const [saved, setSaved] = useState(false);
  const [testResult, setTestResult] = useState('');
  const [testing, setTesting] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    api.get<SmtpSettings>('/api/admin/smtp-settings').then(setSettings).catch((e) => setError(e.message));
  }, []);

  if (!settings) return <p>{t('common.loading')}</p>;

  const save = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setSaved(false);
    try {
      await api.put('/api/admin/smtp-settings', {
        host: settings.host,
        port: settings.port,
        username: settings.username,
        password: password || null,
        securityMode: settings.securityMode,
        senderName: settings.senderName,
        senderEmail: settings.senderEmail,
      });
      setSaved(true);
      setPassword('');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    }
  };

  const sendTest = async () => {
    setTesting(true);
    setTestResult('');
    setError('');
    try {
      const res = await api.post<{ message: string }>('/api/admin/smtp-settings/test', { toEmail: testEmail });
      setTestResult(res.message);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    } finally {
      setTesting(false);
    }
  };

  return (
    <div style={{ maxWidth: 680 }}>
      <h2>{t('admin.smtp')}</h2>
      {error && <div className="error-box">{error}</div>}
      {saved && <div className="success-box">{t('admin.saved')}</div>}
      {testResult && <div className="success-box">{testResult}</div>}

      <form onSubmit={save} className="card" style={{ marginBottom: 18 }}>
        <div className="grid-2">
          <div className="field">
            <label>{t('admin.host')}</label>
            <input dir="ltr" value={settings.host ?? ''} onChange={(e) => setSettings({ ...settings, host: e.target.value })} required />
          </div>
          <div className="field">
            <label>{t('admin.port')}</label>
            <input type="number" dir="ltr" value={settings.port} onChange={(e) => setSettings({ ...settings, port: Number(e.target.value) })} required />
          </div>
          <div className="field">
            <label>{t('admin.username')}</label>
            <input dir="ltr" value={settings.username ?? ''} onChange={(e) => setSettings({ ...settings, username: e.target.value })} />
          </div>
          <div className="field">
            <label>{t('admin.password')}</label>
            <input type="password" dir="ltr" value={password} onChange={(e) => setPassword(e.target.value)}
              placeholder={settings.hasPassword ? '••••••••' : ''} />
          </div>
          <div className="field">
            <label>{t('admin.security')}</label>
            <select value={settings.securityMode} onChange={(e) => setSettings({ ...settings, securityMode: Number(e.target.value) })}>
              <option value={0}>None</option>
              <option value={1}>SSL</option>
              <option value={2}>STARTTLS</option>
            </select>
          </div>
          <div className="field">
            <label>{t('admin.senderName')}</label>
            <input value={settings.senderName ?? ''} onChange={(e) => setSettings({ ...settings, senderName: e.target.value })} required />
          </div>
          <div className="field">
            <label>{t('admin.senderEmail')}</label>
            <input type="email" dir="ltr" value={settings.senderEmail ?? ''} onChange={(e) => setSettings({ ...settings, senderEmail: e.target.value })} required />
          </div>
        </div>
        <button className="btn" type="submit">
          <Save size={16} />
          {t('admin.save')}
        </button>
      </form>

      <div className="card">
        <h3>{t('admin.sendTest')}</h3>
        <div style={{ display: 'flex', gap: 10, alignItems: 'flex-end', flexWrap: 'wrap' }}>
          <div className="field" style={{ flex: 1, minWidth: 220, marginBottom: 0 }}>
            <label>{t('auth.email')}</label>
            <input type="email" dir="ltr" value={testEmail} onChange={(e) => setTestEmail(e.target.value)} />
          </div>
          <button className="btn secondary" onClick={sendTest} disabled={testing || !testEmail}>
            {testing ? <Loader2 size={16} className="spin" /> : <Send size={16} />}
            {t('admin.sendTest')}
          </button>
        </div>
      </div>
    </div>
  );
}

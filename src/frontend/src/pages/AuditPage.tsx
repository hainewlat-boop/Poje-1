import { useHasPermission } from '../store/authStore';

export default function AuditPage() {
  const canViewAudit = useHasPermission('audit:read');

  if (!canViewAudit) {
    return (
      <div className="card">
        <div className="text-center py-12">
          <svg className="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
          </svg>
          <h3 className="mt-2 text-sm font-medium text-gray-900">Erişim Engellendi</h3>
          <p className="mt-1 text-sm text-gray-500">
            Bu sayfayı görüntülemek için yetkiniz bulunmamaktadır.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="card">
        <h1 className="text-2xl font-bold text-gray-900">Denetim Kayıtları</h1>
        <p className="text-gray-600">Sistem aktivite logları</p>
      </div>
      
      <div className="card">
        <p className="text-gray-500">Denetim kayıtları yükleniyor...</p>
      </div>
    </div>
  );
}

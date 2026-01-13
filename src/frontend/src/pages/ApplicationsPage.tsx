import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { applicationsApi } from '../services/api';

interface Application {
  id: string;
  referenceNumber: string;
  applicationTypeCode: string;
  status: string;
  createdAt: string;
  slaDeadline?: string;
}

const statusColors: Record<string, string> = {
  DRAFT: 'bg-gray-100 text-gray-800',
  SUBMITTED: 'bg-blue-100 text-blue-800',
  IN_REVIEW: 'bg-yellow-100 text-yellow-800',
  APPROVED: 'bg-green-100 text-green-800',
  REJECTED: 'bg-red-100 text-red-800',
  PENDING_PAYMENT: 'bg-orange-100 text-orange-800',
  COMPLETED: 'bg-green-100 text-green-800',
};

const statusLabels: Record<string, string> = {
  DRAFT: 'Taslak',
  SUBMITTED: 'Gönderildi',
  IN_REVIEW: 'İncelemede',
  APPROVED: 'Onaylandı',
  REJECTED: 'Reddedildi',
  PENDING_PAYMENT: 'Ödeme Bekliyor',
  COMPLETED: 'Tamamlandı',
};

export default function ApplicationsPage() {
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const { data, isLoading, error } = useQuery({
    queryKey: ['applications', page],
    queryFn: () => applicationsApi.list(page, pageSize),
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg">
        Başvurular yüklenirken bir hata oluştu.
      </div>
    );
  }

  // Mock data for demo
  const applications: Application[] = data?.items || [
    { id: '1', referenceNumber: 'APP-2024-001', applicationTypeCode: 'LICENSE', status: 'SUBMITTED', createdAt: '2024-01-15T10:00:00Z' },
    { id: '2', referenceNumber: 'APP-2024-002', applicationTypeCode: 'PERMIT', status: 'IN_REVIEW', createdAt: '2024-01-14T14:30:00Z' },
    { id: '3', referenceNumber: 'APP-2024-003', applicationTypeCode: 'CERTIFICATE', status: 'PENDING_PAYMENT', createdAt: '2024-01-13T09:15:00Z' },
    { id: '4', referenceNumber: 'APP-2024-004', applicationTypeCode: 'LICENSE', status: 'APPROVED', createdAt: '2024-01-12T16:45:00Z' },
    { id: '5', referenceNumber: 'APP-2024-005', applicationTypeCode: 'PERMIT', status: 'COMPLETED', createdAt: '2024-01-10T11:20:00Z' },
  ];

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Başvurularım</h1>
          <p className="text-gray-600">Tüm başvurularınızı buradan takip edebilirsiniz</p>
        </div>
        <Link to="/applications/new" className="btn-primary">
          + Yeni Başvuru
        </Link>
      </div>

      <div className="card overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Referans No
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Tür
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Durum
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Tarih
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                İşlemler
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {applications.map((app) => (
              <tr key={app.id} className="hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap">
                  <Link to={`/applications/${app.id}`} className="text-primary-600 hover:text-primary-800 font-medium">
                    {app.referenceNumber}
                  </Link>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                  {app.applicationTypeCode}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span className={`px-2 py-1 text-xs font-medium rounded-full ${statusColors[app.status] || 'bg-gray-100 text-gray-800'}`}>
                    {statusLabels[app.status] || app.status}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {new Date(app.createdAt).toLocaleDateString('tr-TR')}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                  <Link to={`/applications/${app.id}`} className="text-primary-600 hover:text-primary-800">
                    Detay
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      <div className="flex justify-between items-center">
        <p className="text-sm text-gray-600">
          Toplam {data?.totalCount || applications.length} başvuru
        </p>
        <div className="flex gap-2">
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="btn-secondary"
          >
            Önceki
          </button>
          <button
            onClick={() => setPage((p) => p + 1)}
            disabled={!data?.hasNextPage}
            className="btn-secondary"
          >
            Sonraki
          </button>
        </div>
      </div>
    </div>
  );
}

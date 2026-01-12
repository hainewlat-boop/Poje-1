import { useParams } from 'react-router-dom';

export default function ApplicationDetailPage() {
  const { id } = useParams();

  return (
    <div className="space-y-6">
      <div className="card">
        <h1 className="text-2xl font-bold text-gray-900">Başvuru Detayı</h1>
        <p className="text-gray-600">Başvuru ID: {id}</p>
      </div>
      
      <div className="card">
        <p className="text-gray-500">Başvuru detayları yükleniyor...</p>
      </div>
    </div>
  );
}

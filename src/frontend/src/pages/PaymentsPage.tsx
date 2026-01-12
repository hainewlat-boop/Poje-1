export default function PaymentsPage() {
  return (
    <div className="space-y-6">
      <div className="card">
        <h1 className="text-2xl font-bold text-gray-900">Ödemeler</h1>
        <p className="text-gray-600">Ödeme geçmişiniz ve bekleyen ödemeler</p>
      </div>
      
      <div className="card">
        <p className="text-gray-500">Ödeme bilgileri yükleniyor...</p>
      </div>
    </div>
  );
}

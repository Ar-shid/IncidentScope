export function Footer() {
  const currentYear = new Date().getFullYear();

  return (
    <footer className="mt-auto py-6 border-t border-white/10 bg-black/20 backdrop-blur-sm">
      <div className="max-w-7xl mx-auto px-8">
        <div className="text-center text-gray-400 text-sm">
          <p>
            © {currentYear} <span className="text-gray-300 font-medium">Rashid Zafar Iqbal</span>. All rights reserved.
          </p>
        </div>
      </div>
    </footer>
  );
}


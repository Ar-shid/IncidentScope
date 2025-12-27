'use client';

import { useEffect, useState } from 'react';
import Image from 'next/image';

export function SplashScreen() {
  const [isLoading, setIsLoading] = useState(true);
  const [isFading, setIsFading] = useState(false);

  useEffect(() => {
    // Wait for page to be fully loaded
    const handleLoad = () => {
      setTimeout(() => {
        setIsFading(true);
        // After fade animation completes, hide the splash
        setTimeout(() => {
          setIsLoading(false);
        }, 500); // Match the fade-out duration
      }, 800); // Minimum display time
    };

    if (document.readyState === 'complete') {
      handleLoad();
    } else {
      window.addEventListener('load', handleLoad);
      return () => window.removeEventListener('load', handleLoad);
    }
  }, []);

  if (!isLoading) return null;

  return (
    <div
      className={`fixed inset-0 z-50 flex items-center justify-center bg-gradient-to-br from-slate-900 via-slate-800 to-slate-950 transition-opacity duration-500 ${
        isFading ? 'opacity-0 pointer-events-none' : 'opacity-100'
      }`}
    >
      <div className="flex flex-col items-center justify-center space-y-12">
        {/* Logo with enhanced styling */}
        <div className="relative w-64 h-64 md:w-80 md:h-80 lg:w-96 lg:h-96">
          <div className="absolute inset-0 bg-white/5 rounded-3xl backdrop-blur-sm border border-white/10 shadow-2xl flex items-center justify-center p-8">
            <Image
              src="/assets/logo.png"
              alt="IncidentScope Logo"
              width={384}
              height={384}
              className="w-full h-full object-contain drop-shadow-2xl"
              unoptimized
            />
          </div>
        </div>

        {/* Loading Spinner */}
        <div className="flex flex-col items-center space-y-6">
          <div className="relative">
            <div className="w-20 h-20 border-4 border-primary-500/20 border-t-primary-500 rounded-full animate-spin shadow-lg"></div>
          </div>
          <p className="text-gray-200 text-lg font-medium animate-pulse">
            Loading IncidentScope...
          </p>
        </div>
      </div>
    </div>
  );
}


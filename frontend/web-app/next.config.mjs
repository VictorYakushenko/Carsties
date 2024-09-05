/** @type {import('next').NextConfig} */
const nextConfig = {
    images: {
        domains: [
            'cdn.pixabay.com'
        ]
    },
    logging: {
        fetches: {
            fullUrl: true
        }
    },
    output: 'standalone'
};

export default nextConfig;

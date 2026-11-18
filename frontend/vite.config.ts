import { readFile, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';

import { transform } from 'esbuild';
import { defineConfig, type Plugin } from 'vite';

const outputDir = resolve(import.meta.dirname, '../src/Jellyfin.Plugin.PremiumUI/Web');

/**
 * Vite desactiva la minificación en modo librería; este plugin la aplica al
 * archivo final. El catálogo del servidor calcula después su nombre hasheado.
 */
function minifyLibraryOutput(): Plugin {
    return {
        name: 'premiumui-minify',
        apply: 'build',
        async closeBundle() {
            const file = resolve(outputDir, 'app.js');
            const source = await readFile(file, 'utf8');
            const result = await transform(source, {
                loader: 'js',
                minify: true,
                format: 'esm',
                target: 'es2020',
                legalComments: 'none'
            });

            if (result.code.length > 0) {
                await writeFile(file, result.code);
            }
        }
    };
}

export default defineConfig({
    plugins: [minifyLibraryOutput()],
    build: {
        outDir: outputDir,
        emptyOutDir: true,
        target: 'es2020',
        cssCodeSplit: false,
        sourcemap: false,
        modulePreload: false,
        lib: {
            entry: resolve(import.meta.dirname, 'src/main.ts'),
            formats: ['es'],
            fileName: () => 'app.js'
        },
        rollupOptions: {
            output: {
                assetFileNames: (assetInfo) => {
                    const name = assetInfo.names?.[0] ?? '';
                    return name.endsWith('.css') ? 'app.css' : '[name][extname]';
                }
            }
        }
    }
});

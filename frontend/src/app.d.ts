// See https://svelte.dev/docs/kit/types#app.d.ts
// for information about these interfaces
declare global {
	namespace App {
		// interface Error {}
		// interface Locals {}
		// interface PageData {}
		// interface PageState {}
		// interface Platform {}
	}
}

interface ImportMetaEnv {
	readonly PUBLIC_API_URL:        string
	readonly PUBLIC_MINIO_URL:      string
	readonly PUBLIC_GRIMOIRE_USER:  string
}

interface ImportMeta {
	readonly env: ImportMetaEnv
}

export {};

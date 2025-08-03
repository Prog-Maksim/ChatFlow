import type { ButtonHTMLAttributes, ReactNode } from 'react';

type Variant = 'primary' | 'secondary';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
	variant?: Variant;
	isLoading?: boolean;
	children: ReactNode;
}

const variantClasses: Record<Variant, string> = {
	primary: 'bg-[#2044EE] text-white hover:bg-[#4361EE] font-bold',
	secondary: 'bg-gray-200 text-black hover:bg-gray-300 font-bold',
};

export function Button({
	variant = 'primary',
	isLoading = false,
	children,
	disabled,
	...rest
}: ButtonProps) {
	return (
		<button
			className={`inline-flex rounded-[10px] cursor-pointer w-[300px] items-center justify-center py-2.5 font-medium transition-colors duration-200 disabled:opacity-50 disabled:cursor-not-allowed ${variantClasses[variant]}`}
			disabled={isLoading || disabled}
			{...rest}
		>
			{isLoading ? 'Загрузка...' : children}
		</button>
	);
}

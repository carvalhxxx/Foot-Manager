interface Props {
  corPrimaria: string
  corSecundaria: string
  tamanho?: 'pequeno' | 'grande'
}

/** "Escudo" simples com as cores do clube (clubes são fictícios: sem escudos de verdade). */
export function Escudo({ corPrimaria, corSecundaria, tamanho = 'pequeno' }: Props) {
  return (
    <span
      className={`escudo escudo-${tamanho}`}
      style={{ background: corPrimaria, borderColor: corSecundaria }}
      aria-hidden="true"
    />
  )
}

from pathlib import Path
import re
import sys
import unicodedata

import pandas as pd


# MANUTENCAO DAS REGRAS
# Altere COLUNA_ROTEAMENTO se o nome do campo mudar.
# Altere ROTAS se os codigos ou as abas de destino mudarem.
COLUNA_ROTEAMENTO = "tipo_atividade"

ROTAS = [
    {
        "tipos_atividade": [1, 2],
        "aba": "Ocorrências Varejo"
    },
    {
        "tipos_atividade": [3],
        "aba": "Ocorrências Saúde"
    }
]


def executar_consulta() -> pd.DataFrame:
    """Carrega a fonte mockada. Futuramente, retorna o DataFrame da AWS."""
    pasta_projeto = Path(__file__).resolve().parent.parent
    arquivo_csv = pasta_projeto / "exemplo" / "dados_exemplo.csv"

    if not arquivo_csv.exists():
        raise FileNotFoundError(f"Arquivo de exemplo não encontrado: {arquivo_csv}")

    return pd.read_csv(
        arquivo_csv,
        sep=",",
        encoding="utf-8-sig",
        decimal=".",
    )


def validar_e_normalizar_coluna_roteamento(
    dataframe: pd.DataFrame,
) -> pd.DataFrame:
    if COLUNA_ROTEAMENTO not in dataframe.columns:
        raise ValueError(
            f"A fonte não retornou a coluna '{COLUNA_ROTEAMENTO}'."
        )

    dataframe = dataframe.copy()
    tipos = pd.to_numeric(dataframe[COLUNA_ROTEAMENTO], errors="coerce")

    if tipos.isna().any():
        linhas = (tipos[tipos.isna()].index + 2).tolist()
        raise ValueError(
            f"Existem valores vazios ou inválidos em '{COLUNA_ROTEAMENTO}' "
            f"nas linhas do CSV: {linhas[:10]}"
        )

    if not (tipos % 1 == 0).all():
        raise ValueError(
            f"Todos os valores de '{COLUNA_ROTEAMENTO}' devem ser inteiros."
        )

    dataframe[COLUNA_ROTEAMENTO] = tipos.astype("int64")
    return dataframe


def nome_arquivo_para_aba(nome_aba: str) -> str:
    nome_sem_acentos = unicodedata.normalize("NFKD", nome_aba)
    nome_sem_acentos = nome_sem_acentos.encode("ascii", "ignore").decode("ascii")
    nome_normalizado = re.sub(r"[^a-zA-Z0-9]+", "_", nome_sem_acentos)
    nome_normalizado = nome_normalizado.strip("_").lower()

    if not nome_normalizado:
        raise ValueError(
            f"Não foi possível gerar um nome de arquivo para a aba: {nome_aba}"
        )

    return f"resultado_{nome_normalizado}.csv"


def validar_rotas(dataframe: pd.DataFrame) -> None:
    tipos_configurados = []
    arquivos_configurados = []

    for rota in ROTAS:
        tipos_configurados.extend(rota["tipos_atividade"])

        nome_aba = rota["aba"]
        if len(nome_aba) > 31 or any(caractere in nome_aba for caractere in "[]:*?/\\"):
            raise ValueError(f"Nome de aba inválido para o Excel: {nome_aba}")

        arquivos_configurados.append(nome_arquivo_para_aba(nome_aba))

    if len(tipos_configurados) != len(set(tipos_configurados)):
        raise ValueError(
            f"Um valor de '{COLUNA_ROTEAMENTO}' foi configurado em mais de uma rota."
        )

    if len(arquivos_configurados) != len(set(arquivos_configurados)):
        raise ValueError(
            "Duas abas diferentes geraram o mesmo nome de arquivo de resultado."
        )

    tipos_retornados = set(dataframe[COLUNA_ROTEAMENTO].unique())
    tipos_sem_rota = tipos_retornados - set(tipos_configurados)

    if tipos_sem_rota:
        quantidades_sem_rota = (
            dataframe.loc[
                dataframe[COLUNA_ROTEAMENTO].isin(tipos_sem_rota),
                COLUNA_ROTEAMENTO,
            ]
            .value_counts()
            .sort_index()
            .to_dict()
        )

        print(
            "Detalhe técnico — tipos sem rota configurada: "
            f"{quantidades_sem_rota}",
            file=sys.stderr,
        )

        raise ValueError(
            "Os dados retornados estão fora da configuração esperada. "
            "Entre em contato com o responsável pelo processo."
        )


def remover_resultados_anteriores(pasta_saida: Path) -> None:
    pasta_saida = pasta_saida.resolve()
    manifesto_anterior = pasta_saida / "manifesto.csv"
    arquivos_para_remover = {"resultado.csv"}

    if manifesto_anterior.exists():
        manifesto = pd.read_csv(
            manifesto_anterior,
            sep=";",
            encoding="utf-8-sig",
        )

        if "arquivo" not in manifesto.columns:
            raise ValueError(
                "O manifesto anterior não possui a coluna obrigatória 'arquivo'."
            )

        arquivos_para_remover.update(
            str(nome) for nome in manifesto["arquivo"].dropna()
        )

    for nome_arquivo in arquivos_para_remover:
        caminho_arquivo = (pasta_saida / nome_arquivo).resolve()

        if caminho_arquivo.parent != pasta_saida:
            raise ValueError(
                f"Caminho inválido encontrado no manifesto: {nome_arquivo}"
            )

        if caminho_arquivo.suffix.lower() != ".csv":
            raise ValueError(
                f"O manifesto tentou remover um arquivo que não é CSV: {nome_arquivo}"
            )

        if caminho_arquivo.exists():
            caminho_arquivo.unlink()

    if manifesto_anterior.exists():
        manifesto_anterior.unlink()


def gerar_resultados(dataframe: pd.DataFrame, pasta_saida: Path) -> None:
    pasta_saida.mkdir(parents=True, exist_ok=True)
    remover_resultados_anteriores(pasta_saida)
    instrucoes = []

    for rota in ROTAS:
        nome_arquivo = nome_arquivo_para_aba(rota["aba"])
        dados_filtrados = dataframe[
            dataframe[COLUNA_ROTEAMENTO].isin(rota["tipos_atividade"])
        ].copy()

        caminho_saida = pasta_saida / nome_arquivo
        dados_filtrados.to_csv(
            caminho_saida,
            index=False,
            sep=";",
            encoding="utf-8-sig",
            decimal=",",
        )

        instrucoes.append(
            {
                "arquivo": nome_arquivo,
                "aba": rota["aba"],
            }
        )

        print(
            f"{rota['aba']}: {len(dados_filtrados)} registros "
            f"em {caminho_saida}"
        )

    pd.DataFrame(instrucoes).to_csv(
        pasta_saida / "manifesto.csv",
        index=False,
        sep=";",
        encoding="utf-8-sig",
    )


def obter_pasta_saida() -> Path:
    pasta_projeto = Path(__file__).resolve().parent.parent

    # Aceita tanto uma pasta quanto o antigo caminho resultado.csv.
    if len(sys.argv) >= 2:
        argumento_saida = Path(sys.argv[1]).resolve()
        return argumento_saida.parent if argumento_saida.suffix else argumento_saida

    return pasta_projeto


def main() -> None:
    pasta_saida = obter_pasta_saida()
    arquivo_erro = pasta_saida / "erro_consulta.txt"

    if arquivo_erro.exists():
        arquivo_erro.unlink()

    dataframe = executar_consulta()
    dataframe = validar_e_normalizar_coluna_roteamento(dataframe)
    validar_rotas(dataframe)
    gerar_resultados(dataframe, pasta_saida)


if __name__ == "__main__":
    try:
        main()
    except Exception as erro:
        try:
            pasta_saida = obter_pasta_saida()
            pasta_saida.mkdir(parents=True, exist_ok=True)
            (pasta_saida / "erro_consulta.txt").write_text(
                str(erro),
                encoding="utf-8",
            )
        except Exception:
            pass

        raise

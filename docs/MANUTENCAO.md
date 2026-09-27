# Manutenção e publicação

## Fontes de manutenção

| Alteração | Arquivo principal |
|---|---|
| Consulta e DataFrame | `python/consulta.py` |
| Nome da coluna de roteamento | `COLUNA_ROTEAMENTO` em `python/consulta.py` |
| Tipos e abas de destino | `ROTAS` em `python/consulta.py` |
| Comportamento da macro | `vba/ModuloConsulta.bas` e depois `consulta.xlsm` |
| Dependências Python | `configuracao/bibliotecas/requirements.txt` |
| Proxies, certificado e arquivos | `configuracao/ambiente.json` |
| Conteúdo e comportamento do EXE | `build/Program.cs` |
| Ícone | `build/ocorrencias.ico` |
| Processo de compilação | `exe/gerar-exe.ps1` |

## Alterar a coluna de roteamento

Edite somente:

```python
COLUNA_ROTEAMENTO = "novo_nome_do_campo"
```

O nome deve corresponder exatamente à coluna devolvida pelo analítico. Se ela não existir, a consulta falha com mensagem controlada.

## Alterar tipos e planilhas

Exemplo:

```python
ROTAS = [
    {"tipos_atividade": [1, 2], "aba": "Ocorrências Varejo"},
    {"tipos_atividade": [3], "aba": "Ocorrências Saúde"},
]
```

Regras:

- um tipo não pode aparecer em mais de uma rota;
- todo tipo retornado deve possuir rota;
- nomes de abas devem respeitar o limite de 31 caracteres do Excel;
- nomes de abas não podem conter `[]:*?/\`;
- duas abas não podem gerar o mesmo nome normalizado de CSV.

O nome do CSV é derivado da aba. `Ocorrências Saúde`, por exemplo, gera `resultado_ocorrencias_saude.csv`.

## Substituir o mock pela AWS

Altere a implementação de `executar_consulta()` e preserve seu retorno como `pandas.DataFrame`. Não coloque regras de planilha dentro da consulta AWS; mantenha o roteamento nas constantes de manutenção.

Antes da consulta, incorpore o fluxo descrito em [AWS SSO e Glue](AWS_E_GLUE.md). O código mockado atual não realiza autenticação.

## Atualizar o VBA

`vba/ModuloConsulta.bas` é a fonte de manutenção, mas o gerador inclui `consulta.xlsm` exatamente como ela estiver. Após modificar o `.bas`:

1. abra `consulta.xlsm`;
2. pressione `Alt + F11`;
3. remova o módulo antigo `ModuloConsulta`;
4. importe `vba/ModuloConsulta.bas`;
5. salve mantendo o formato `.xlsm`;
6. teste o botão antes de compilar.

Se a empresa exigir assinatura da macro, assine novamente após qualquer alteração.

## Configurar o ambiente corporativo

Siga [Configuração do ambiente corporativo](../configuracao/LEIA-ME.md). Antes de habilitar:

- confirme caminhos reais do perfil AWS, certificado e `pip.ini`;
- preencha proxies sem incluir credenciais indevidas;
- fixe versões das bibliotecas homologadas;
- teste em uma conta Windows descartável ou máquina de homologação;
- valide o comportamento quando itens já existem.

O código preserva configurações existentes. Isso evita sobrescrever o computador do analista, mas significa que valores antigos não são corrigidos automaticamente.

## Checklist de teste

### Consulta e roteamento

- DataFrame com tipos 1, 2 e 3.
- DataFrame com colunas adicionais e ordem diferente.
- Coluna de roteamento ausente.
- Valor vazio, texto ou decimal na coluna de roteamento.
- Tipo válido, mas sem rota.
- Rota duplicada.
- Aba com nome inválido.
- Destino sem registros, confirmando se a aba vazia é esperada.

### Excel

- Botão chama `ExecutarConsultaPython`.
- Abas anteriores são limpas antes da importação.
- Acentos e casas decimais são importados corretamente.
- Horário muda somente após sucesso.
- Sucesso não exibe caixa de mensagem.
- Erro exibe mensagem genérica e preserva o horário anterior.

### Pacote

- Primeira execução em usuário limpo.
- Segunda execução não repete a preparação.
- Nova versão atualiza o payload.
- Planilha aberta bloqueia a atualização de forma compreensível.
- Python diferente de 3.12 não é aceito.
- Falha do pip não cria `.ambiente-configurado`.
- `Path`, arquivos e variáveis existentes são preservados.

## Publicação

1. Atualize código, documentação e versões homologadas.
2. Execute os testes locais com o CSV mockado.
3. Importe o VBA atualizado em `consulta.xlsm`.
4. Feche o Excel.
5. Execute `./exe/gerar-exe.ps1` no PowerShell.
6. Confirme data, tamanho e hash de `dist/Ocorrencias.exe`.
7. Teste o EXE em um usuário de homologação.
8. Assine o executável e a macro conforme a política corporativa.
9. Distribua somente o EXE aprovado.

O gerador sobrescreve `dist/Ocorrencias.exe`; ele não cria um nome de arquivo diferente a cada compilação.

## Versionamento recomendado

Antes da versão final, defina um processo para:

- número de versão do assembly em `build/Program.cs`;
- registro de alterações (`CHANGELOG.md`);
- hash do artefato distribuído;
- versões fixas no `requirements.txt`;
- identificação da versão da planilha em uma célula visível ou propriedade do arquivo.


﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ET.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EntityMemberDeclarationAnalyzer: DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>ImmutableArray.Create(EntityDelegateDeclarationAnalyzerRule.Rule,
            EntityFieldDeclarationInEntityAnalyzerRule.Rule, LSEntityFloatMemberAnalyzer.Rule);
        
        public override void Initialize(AnalysisContext context)
        {
            if (!AnalyzerGlobalSetting.EnableAnalyzer)
            {
                return;
            }
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(this.Analyzer, SymbolKind.NamedType);
        }

        private void Analyzer(SymbolAnalysisContext context)
        {
            if (!AnalyzerHelper.IsAssemblyNeedAnalyze(context.Compilation.AssemblyName, AnalyzeAssembly.AllModel))
            {
                return;
            }
            
            if (!(context.Symbol is INamedTypeSymbol namedTypeSymbol))
            {
                return;
            }

            var baseType = namedTypeSymbol.BaseType?.ToString();
            // 筛选出实体类
            if (baseType== Definition.EntityType)
            {
                AnalyzeDelegateMember(context, namedTypeSymbol);
                AnalyzeEntityMember(context, namedTypeSymbol);
            }else if (baseType == Definition.LSEntityType)
            {
                AnalyzeDelegateMember(context, namedTypeSymbol);
                AnalyzeEntityMember(context, namedTypeSymbol);
                AnalyzeFloatMemberInLSEntity(context,namedTypeSymbol);
            }
        }

        /// <summary>
        /// 检查委托成员
        /// </summary>
        private void AnalyzeDelegateMember(SymbolAnalysisContext context,INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var member in namedTypeSymbol.GetMembers())
            {
                
                if (member is IFieldSymbol fieldSymbol && fieldSymbol.Type.BaseType?.ToString()==typeof(MulticastDelegate).FullName)
                {

                    ReportDiagnostic(fieldSymbol,fieldSymbol.Name);
                    continue;
                }
                
                if (member is IPropertySymbol propertySymbol && propertySymbol.Type.BaseType?.ToString()==typeof(MulticastDelegate).FullName)
                {

                    ReportDiagnostic(propertySymbol,propertySymbol.Name);
                    continue;
                }
            }
            
            void ReportDiagnostic(ISymbol symbol,string delegateName)
            {
                foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
                {
                    var syntax = syntaxReference.GetSyntax();
                    Diagnostic diagnostic = Diagnostic.Create(EntityDelegateDeclarationAnalyzerRule.Rule, syntax.GetLocation(),namedTypeSymbol.Name,delegateName);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// 检查实体成员
        /// </summary>
        private void AnalyzeEntityMember(SymbolAnalysisContext context, INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var member in namedTypeSymbol.GetMembers())
            {
                if (member is not IFieldSymbol fieldSymbol)
                {
                    continue;
                }

                // 忽略静态字段 允许单例实体类
                if (fieldSymbol.IsStatic)
                {
                    continue;
                }
                if (fieldSymbol.Type.ToString()is Definition.EntityType or Definition.LSEntityType || fieldSymbol.Type.BaseType?.ToString()is Definition.EntityType or Definition.LSEntityType)
                {
                    var syntaxReference = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                    if (syntaxReference==null)
                    {
                        continue;
                    }
                    Diagnostic diagnostic = Diagnostic.Create(EntityFieldDeclarationInEntityAnalyzerRule.Rule, syntaxReference.GetSyntax().GetLocation(),namedTypeSymbol.Name,fieldSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// 检查LSEntity中 是否有浮点数字段
        /// </summary>
        private void AnalyzeFloatMemberInLSEntity(SymbolAnalysisContext context, INamedTypeSymbol namedTypeSymbol)
        {
            
            foreach (var member in namedTypeSymbol.GetMembers())
            {
                INamedTypeSymbol? memberType = null;
                
                if (member is IFieldSymbol fieldSymbol)
                {
                    memberType = fieldSymbol.Type as INamedTypeSymbol;
                }

                if (member is IPropertySymbol propertySymbol)
                {
                    memberType = propertySymbol.Type as INamedTypeSymbol;
                }

                if (memberType==null)
                {
                    continue;
                }
                
                if (memberType.SpecialType is  SpecialType.System_Single or SpecialType.System_Double)
                {
                    var syntaxReference = member.DeclaringSyntaxReferences.FirstOrDefault();
                    if (syntaxReference==null)
                    {
                        continue;
                    }
                    Diagnostic diagnostic = Diagnostic.Create(LSEntityFloatMemberAnalyzer.Rule, syntaxReference.GetSyntax().GetLocation(),namedTypeSymbol.Name,member.Name);
                    context.ReportDiagnostic(diagnostic);
                    continue;
                }

                if (memberType.IsGenericType && GenericTypeHasFloatTypeArgs(memberType))
                {
                    var syntaxReference = member.DeclaringSyntaxReferences.FirstOrDefault();
                    if (syntaxReference==null)
                    {
                        continue;
                    }
                    Diagnostic diagnostic = Diagnostic.Create(LSEntityFloatMemberAnalyzer.Rule, syntaxReference.GetSyntax().GetLocation(),namedTypeSymbol.Name,member.Name);
                    context.ReportDiagnostic(diagnostic);
                    continue;
                }
            }
        }


        /// <summary>
        /// 泛型类 是否含有浮点数类型参数
        /// 对于嵌套泛型参数 递归判断
        /// </summary>
        private bool GenericTypeHasFloatTypeArgs(INamedTypeSymbol namedTypeSymbol)
        {
            var typeArgs = namedTypeSymbol.TypeArguments;
            foreach (var typeSymbol in typeArgs)
            {
                if (typeSymbol is not INamedTypeSymbol namedTypeSymbol2)
                {
                    break;
                }

                if (namedTypeSymbol2.IsGenericType)
                {
                    if (GenericTypeHasFloatTypeArgs(namedTypeSymbol2))
                    {
                        return true;
                    }
                }
                else
                {
                    if (namedTypeSymbol2.SpecialType is SpecialType.System_Single or SpecialType.System_Double)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}


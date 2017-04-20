using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GraphView
{
    internal class GremlinOrderOp: GremlinTranslationOperator
    {
        public List<Tuple<GraphTraversal2, IComparer>> ByModulatingList { get; set; } = new List<Tuple<GraphTraversal2, IComparer>>();

        public override void ModulateBy(GraphTraversal2 traversal)
        {
            this.ByModulatingList.Add(new Tuple<GraphTraversal2, IComparer>(traversal, new IncrOrder()));
        }

        public override void ModulateBy(GraphTraversal2 traversal, IComparer comparer)
        {
            this.ByModulatingList.Add(new Tuple<GraphTraversal2, IComparer>(traversal, comparer));
        }
    }

    internal class GremlinOrderGlobalOp : GremlinOrderOp
    {
        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            if (this.ByModulatingList.Count == 0)
            {
                this.ByModulatingList.Add(new Tuple<GraphTraversal2, IComparer>(GraphTraversal2.__(), new IncrOrder()));
            }

            var newByModulatingList = new List<Tuple<GremlinToSqlContext, IComparer>>();
            foreach (var pair in this.ByModulatingList)
            {
                GraphTraversal2 traversal = pair.Item1;
                GremlinToSqlContext context = null;

                traversal.GetStartOp().InheritedVariableFromParent(inputContext);
                context = traversal.GetEndOp().GetContext();

                newByModulatingList.Add(new Tuple<GremlinToSqlContext, IComparer>(context, pair.Item2));
            }

            inputContext.PivotVariable.OrderGlobal(inputContext, newByModulatingList);

            return inputContext;
        }
    }


    internal class GremlinOrderLocalOp : GremlinOrderOp
    {
        internal override GremlinToSqlContext GetContext()
        {
            GremlinToSqlContext inputContext = GetInputContext();
            if (inputContext.PivotVariable == null)
            {
                throw new QueryCompilationException("The PivotVariable can't be null.");
            }

            if (this.ByModulatingList.Count == 0)
            {
                this.ByModulatingList.Add(new Tuple<GraphTraversal2, IComparer>(GraphTraversal2.__(), new IncrOrder()));
            }

            var newByModulatingList = new List<Tuple<GremlinToSqlContext, IComparer>>();
            foreach (var pair in this.ByModulatingList)
            {
                GraphTraversal2 traversal = pair.Item1;
                GremlinToSqlContext context = null;

                //g.V().groupCount().order(Local).by(Keys) or g.V().groupCount().order(Local).by(__.select(Keys))
                if (traversal.TranslationOpList.Count >= 2 && traversal.TranslationOpList[1] is GremlinSelectColumnOp)
                {
                    //FROM selectColumn(C._value, "Keys"/"Values")
                    GremlinToSqlContext newContext = new GremlinToSqlContext();
                    GremlinOrderLocalInitVariable initVar = new GremlinOrderLocalInitVariable();
                    newContext.VariableList.Add(initVar);
                    newContext.SetPivotVariable(initVar);

                    traversal.GetStartOp().InheritedContextFromParent(newContext);
                    context = traversal.GetEndOp().GetContext();
                }
                else
                {
                    //FROM decompose1(C._value)
                    GremlinToSqlContext newContext = new GremlinToSqlContext();
                    GremlinDecompose1Variable decompose1 = new GremlinDecompose1Variable(inputContext.PivotVariable);
                    newContext.VariableList.Add(decompose1);
                    newContext.TableReferences.Add(decompose1);
                    newContext.SetPivotVariable(decompose1);

                    traversal.GetStartOp().InheritedContextFromParent(newContext);
                    context = traversal.GetEndOp().GetContext();
                }

                newByModulatingList.Add(new Tuple<GremlinToSqlContext, IComparer>(context, pair.Item2));
            }

            inputContext.PivotVariable.OrderLocal(inputContext, newByModulatingList);

            return inputContext;
        }
    }
}

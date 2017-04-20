using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinFreeVertexVariable : GremlinVertexTableVariable
    {
        private bool isTraversalToBound;

        public override WTableReference ToTableReference()
        {
            return new WNamedTableReference()
            {
                Alias = SqlUtil.GetIdentifier(GetVariableName()),
                TableObjectString = "node",
                TableObjectName = SqlUtil.GetSchemaObjectName("node"),
            }; ;
        }

        internal override void Both(GremlinToSqlContext currentContext, List<string> edgeLabels)
        {
            if (this.isTraversalToBound)
            {
                base.Both(currentContext, edgeLabels);
                return;
            }
            GremlinFreeEdgeVariable bothEdge = new GremlinFreeEdgeVariable(WEdgeType.BothEdge);
            currentContext.VariableList.Add(bothEdge);
            currentContext.AddLabelPredicateForEdge(bothEdge, edgeLabels);

            GremlinFreeVertexVariable bothVertex = new GremlinFreeVertexVariable();
            currentContext.VariableList.Add(bothVertex);

            // In this case, the both-edgeTable variable is not added to the table-reference list. 
            // Instead, we populate a path this_variable-[bothEdge]->bothVertex in the context
            currentContext.TableReferences.Add(bothVertex);
            currentContext.MatchPathList.Add(new GremlinMatchPath(this, bothEdge, bothVertex));
            currentContext.SetPivotVariable(bothVertex);
        }

        internal override void In(GremlinToSqlContext currentContext, List<string> edgeLabels)
        {
            if (this.isTraversalToBound)
            {
                base.In(currentContext, edgeLabels);
                return;
            }
            GremlinFreeEdgeVariable inEdge = new GremlinFreeEdgeVariable(WEdgeType.InEdge);
            currentContext.VariableList.Add(inEdge);
            currentContext.AddLabelPredicateForEdge(inEdge, edgeLabels);

            GremlinFreeVertexVariable outVertex = new GremlinFreeVertexVariable();
            currentContext.VariableList.Add(outVertex);
            currentContext.TableReferences.Add(outVertex);
            currentContext.MatchPathList.Add(new GremlinMatchPath(outVertex, inEdge, this));
            currentContext.SetPivotVariable(outVertex);
        }

        internal override void InE(GremlinToSqlContext currentContext, List<string> edgeLabels)
        {
            if (this.isTraversalToBound)
            {
                base.InE(currentContext, edgeLabels);
                return;
            }
            GremlinFreeEdgeVariable inEdge = new GremlinFreeEdgeVariable(WEdgeType.InEdge);
            currentContext.VariableList.Add(inEdge);
            currentContext.AddLabelPredicateForEdge(inEdge, edgeLabels);
            currentContext.MatchPathList.Add(new GremlinMatchPath(null, inEdge, this));
            currentContext.SetPivotVariable(inEdge);
        }

        internal override void Out(GremlinToSqlContext currentContext, List<string> edgeLabels)
        {
            if (this.isTraversalToBound)
            {
                base.Out(currentContext, edgeLabels);
                return;
            }
            GremlinFreeEdgeVariable outEdge = new GremlinFreeEdgeVariable(WEdgeType.OutEdge);
            currentContext.VariableList.Add(outEdge);
            currentContext.AddLabelPredicateForEdge(outEdge, edgeLabels);

            GremlinFreeVertexVariable inVertex = new GremlinFreeVertexVariable();
            currentContext.VariableList.Add(inVertex);
            currentContext.TableReferences.Add(inVertex);
            currentContext.MatchPathList.Add(new GremlinMatchPath(this, outEdge, inVertex));
            currentContext.SetPivotVariable(inVertex);
        }
        internal override void OutE(GremlinToSqlContext currentContext, List<string> edgeLabels)
        {
            if (this.isTraversalToBound)
            {
                base.OutE(currentContext, edgeLabels);
                return;
            }
            GremlinFreeEdgeVariable outEdgeVar = new GremlinFreeEdgeVariable(WEdgeType.OutEdge);
            currentContext.VariableList.Add(outEdgeVar);
            currentContext.AddLabelPredicateForEdge(outEdgeVar, edgeLabels);
            currentContext.MatchPathList.Add(new GremlinMatchPath(this, outEdgeVar, null));
            currentContext.SetPivotVariable(outEdgeVar);
        }

        internal override void Aggregate(GremlinToSqlContext currentContext, string sideEffectKey, GremlinToSqlContext projectContext)
        {
            this.isTraversalToBound = true;
            base.Aggregate(currentContext, sideEffectKey, projectContext);
        }

        internal override void Barrier(GremlinToSqlContext currentContext)
        {
            this.isTraversalToBound = true;
            base.Barrier(currentContext);
        }

        internal override void Coin(GremlinToSqlContext currentContext, double probability)
        {
            this.isTraversalToBound = true;
            base.Coin(currentContext, probability);
        }

        internal override void CyclicPath(GremlinToSqlContext currentContext)
        {
            this.isTraversalToBound = true;
            base.CyclicPath(currentContext);
        }

        internal override void DedupGlobal(GremlinToSqlContext currentContext, List<string> dedupLabels, GraphTraversal2 dedupTraversal)
        {
            this.isTraversalToBound = true;
            base.DedupGlobal(currentContext, dedupLabels, dedupTraversal);
        }

        internal override void DedupLocal(GremlinToSqlContext currentContext, GremlinToSqlContext dedupContext)
        {
            this.isTraversalToBound = true;
            base.DedupLocal(currentContext, dedupContext);
        }

        internal override void Group(GremlinToSqlContext currentContext, string sideEffectKey, GremlinToSqlContext groupByContext,
            GremlinToSqlContext projectByContext, bool isProjectByString)
        {
            if (sideEffectKey != null) this.isTraversalToBound = true;
            base.Group(currentContext, sideEffectKey, groupByContext, projectByContext, isProjectByString);
        }

        internal override void Inject(GremlinToSqlContext currentContext, object injection)
        {
            this.isTraversalToBound = true;
            base.Inject(currentContext, injection);
        }

        internal override void OrderGlobal(GremlinToSqlContext currentContext, List<Tuple<GremlinToSqlContext, IComparer>> byModulatingMap)
        {
            this.isTraversalToBound = true;
            base.OrderGlobal(currentContext, byModulatingMap);
        }

        internal override void OrderLocal(GremlinToSqlContext currentContext, List<Tuple<GremlinToSqlContext, IComparer>> byModulatingMap)
        {
            this.isTraversalToBound = true;
            base.OrderLocal(currentContext, byModulatingMap);
        }

        internal override void Property(GremlinToSqlContext currentContext, GremlinProperty vertexProperty)
        {
            this.isTraversalToBound = true;
            base.Property(currentContext, vertexProperty);
        }

        internal override void RangeLocal(GremlinToSqlContext currentContext, int low, int high, bool isReverse)
        {
            this.isTraversalToBound = true;
            base.RangeLocal(currentContext, low, high, isReverse);
        }

        internal override void RangeGlobal(GremlinToSqlContext currentContext, int low, int high, bool isReverse)
        {
            this.isTraversalToBound = true;
            base.RangeGlobal(currentContext, low, high, isReverse);
        }

        internal override void SampleGlobal(GremlinToSqlContext currentContext, int amountToSample,
            GremlinToSqlContext probabilityContext)
        {
            this.isTraversalToBound = true;
            base.SampleGlobal(currentContext, amountToSample, probabilityContext);
        }

        internal override void SampleLocal(GremlinToSqlContext currentContext, int amountToSample)
        {
            this.isTraversalToBound = true;
            base.SampleLocal(currentContext, amountToSample);
        }

        internal override void SideEffect(GremlinToSqlContext currentContext, GremlinToSqlContext sideEffectContext)
        {
            this.isTraversalToBound = true;
            base.SideEffect(currentContext, sideEffectContext);
        }

        internal override void SimplePath(GremlinToSqlContext currentContext)
        {
            this.isTraversalToBound = true;
            base.SimplePath(currentContext);
        }

        internal override void Store(GremlinToSqlContext currentContext, string sideEffectKey, GremlinToSqlContext projectContext)
        {
            this.isTraversalToBound = true;
            base.Store(currentContext, sideEffectKey, projectContext);
        }

       
        internal override void Tree(GremlinToSqlContext currentContext, string sideEffectKey, List<GraphTraversal2> byList)
        {
            this.isTraversalToBound = true;
            base.Tree(currentContext, sideEffectKey, byList);
        }
    }
}
